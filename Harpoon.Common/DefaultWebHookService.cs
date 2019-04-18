using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon
{
    public class DefaultWebHookService : IWebHookService
    {
        private readonly IWebHookStore _webHookStore;
        private readonly IWebHookSender _webHookSender;

        public DefaultWebHookService(IWebHookStore webHookStore, IWebHookSender webHookSender)
        {
            _webHookStore = webHookStore ?? throw new ArgumentNullException(nameof(webHookStore));
            _webHookSender = webHookSender ?? throw new ArgumentNullException(nameof(webHookSender));
        }

        public async Task<int> NotifyAsync(IWebHookNotification notification, CancellationToken token)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var webHooks = await _webHookStore.GetAllWebHooksAsync(notification.ActionId);
            var filteredWebHooks = webHooks.Where(w => FiltersMatch(w, notification)).ToList();

            if (filteredWebHooks.Count != 0)
            {
                await _webHookSender.SendAsync(notification, filteredWebHooks, token);
            }

            return filteredWebHooks.Count;
        }

        protected virtual bool FiltersMatch(IWebHook webHook, IWebHookNotification notification)
        {
            if (webHook.Filters == null)
            {
                throw new InvalidOperationException("WebHook need to expose filters to be considered valid and ready to be send");
            }

            return webHook.Filters
                .Where(f => f.ActionId == notification.ActionId)
                .Any(f => f.Parameters == null || f.Parameters.Count == 0 || f.Parameters.All(kvp => notification.Payload.ContainsKey(kvp.Key) && notification.Payload[kvp.Key].Equals(kvp.Value)));
        }
    }
}