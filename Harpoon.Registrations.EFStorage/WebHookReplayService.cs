using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Harpoon.Registrations.EFStorage
{
    /// <summary>
    /// A class able to replay failed notifications
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public class WebHookReplayService<TContext>
        where TContext : DbContext, IRegistrationsContext
    {
        private readonly TContext _context;
        private readonly IWebHookSender _sender;
        private readonly ISecretProtector _secretProtector;

        /// <summary>Initializes a new instance of the <see cref="WebHookReplayService{TContext}"/> class.</summary>
        public WebHookReplayService(TContext context, IWebHookSender sender, ISecretProtector secretProtector)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _secretProtector = secretProtector ?? throw new ArgumentNullException(nameof(secretProtector));
        }

        /// <summary>
        /// Replays every failed notification
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public async Task ReplayFailedNotification(DateTime start)
        {
            var failedNotifications = await _context.WebHookLogs
                .Where(l => l.Error != null && l.CreatedAt >= start)
                .Include(e => e.WebHookNotification)
                .Include(e => e.WebHook)
                .AsNoTracking()
                .ToListAsync();

            foreach (var fail in failedNotifications)
            {
                var hasSuccesfulLogs = await _context.WebHookLogs
                    .Where(l => l.WebHookNotificationId == fail.WebHookNotificationId
                        && l.WebHookId == fail.WebHookId
                        && l.CreatedAt > fail.CreatedAt
                        && l.Error == null).AnyAsync();

                if (!hasSuccesfulLogs)
                {
                    fail.WebHook.Secret = _secretProtector.Unprotect(fail.WebHook.ProtectedSecret);
                    await _sender.SendAsync(new WebHookWorkItem(fail.WebHookNotificationId, fail.WebHookNotification, fail.WebHook), default);
                }
            }
        }
    }
}