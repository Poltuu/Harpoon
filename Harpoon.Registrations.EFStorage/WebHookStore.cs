using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Registrations.EFStorage
{
    /// <summary>
    /// Default <see cref="IWebHookStore"/> implementation using EF
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public class WebHookStore<TContext> : IWebHookStore
        where TContext : DbContext, IRegistrationsContext
    {
        private readonly TContext _context;
        private readonly ISecretProtector _secretProtector;

        /// <summary>Initializes a new instance of the <see cref="WebHookStore{TContext}"/> class.</summary>
        public WebHookStore(TContext context, ISecretProtector secretProtector)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _secretProtector = secretProtector ?? throw new ArgumentNullException(nameof(secretProtector));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<IWebHook>> GetApplicableWebHooksAsync(IWebHookNotification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var webHooks = await FilterQuery(_context.WebHooks.AsNoTracking()
                .Where(w => !w.IsPaused)
                .Include(w => w.Filters), notification)
                .ToListAsync(cancellationToken);

            foreach (var webHook in webHooks)
            {
                webHook.Secret = _secretProtector.Unprotect(webHook.ProtectedSecret);
            }
            return webHooks;
        }

        /// <summary>
        /// Apply the SQL filter matching the current notification
        /// </summary>
        /// <param name="query"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
        protected virtual IQueryable<WebHook> FilterQuery(IQueryable<WebHook> query, IWebHookNotification notification)
        {
            return query.Where(w => w.Filters.Count == 0 || w.Filters.Any(f => f.Trigger == notification.TriggerId));
        }
    }
}