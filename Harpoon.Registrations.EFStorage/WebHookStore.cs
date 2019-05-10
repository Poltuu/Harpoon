using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Harpoon.Registrations.EFStorage
{
    public class WebHookStore<TContext> : IWebHookStore
        where TContext : DbContext, IRegistrationsContext
    {
        private readonly TContext _context;
        private readonly ISecretProtector _secretProtector;

        public WebHookStore(TContext context, ISecretProtector secretProtector)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _secretProtector = secretProtector ?? throw new ArgumentNullException(nameof(secretProtector));
        }

        public async Task<IReadOnlyList<IWebHook>> GetApplicableWebHooksAsync(IWebHookNotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var webHooks = await FilterQuery(_context.WebHooks.AsNoTracking().Where(w => !w.IsPaused).Include(w => w.Filters), notification).ToListAsync();

            var result = new List<IWebHook>();
            foreach (var webHook in webHooks.Where(w => FiltersMatch(w, notification)))
            {
                webHook.Secret = _secretProtector.Unprotect(webHook.ProtectedSecret);
                webHook.Callback = new Uri(_secretProtector.Unprotect(webHook.ProtectedCallback));
                result.Add(webHook);
            }
            return result;
        }

        /// <summary>
        /// Apply filter matching current notification
        /// </summary>
        /// <param name="query"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
        protected virtual IQueryable<WebHook> FilterQuery(IQueryable<WebHook> query, IWebHookNotification notification)
        {
            return query.Where(w => w.Filters.Any(f => f.TriggerId == notification.TriggerId));
        }

        /// <summary>
        /// Apply any filter that could not be done in query, such as parameters matching
        /// </summary>
        /// <param name="webHook"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
        protected virtual bool FiltersMatch(WebHook webHook, IWebHookNotification notification)
        {
            if (webHook.Filters == null || webHook.Filters.Count == 0)
            {
                return true;
            }

            return webHook.Filters.Any(f => f.Parameters == null || f.Parameters.Count == 0 || f.Parameters.All(kvp => notification.Payload.ContainsKey(kvp.Key) && notification.Payload[kvp.Key].Equals(kvp.Value)));
        }
    }
}