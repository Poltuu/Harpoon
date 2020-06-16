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

        /// <summary>Initializes a new instance of the <see cref="WebHookReplayService{TContext}"/> class.</summary>
        public WebHookReplayService(TContext context, IWebHookSender sender)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
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
                .ToListAsync();

            foreach (var fail in failedNotifications)
            {
                await _sender.SendAsync(new WebHookWorkItem(fail.WebHookNotificationId, fail.WebHookNotification, fail.WebHook), default);
            }
        }
    }
}