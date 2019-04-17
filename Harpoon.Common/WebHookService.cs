using System;
using System.Linq;
using System.Threading.Tasks;

namespace Harpoon
{
    public class WebHookService : IWebHookService
    {
        private readonly IWebHookStore _webHookStore;
        private readonly IWebHookSender _webHookSender;

        public WebHookService(IWebHookStore webHookStore, IWebHookSender webHookSender)
        {
            _webHookStore = webHookStore ?? throw new ArgumentNullException(nameof(webHookStore));
            _webHookSender = webHookSender ?? throw new ArgumentNullException(nameof(webHookSender));
        }

        public async Task<int> NotifyAsync(IWebHookNotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var webHooks = await _webHookStore.GetAllWebHooksAsync(notification.ActionId);

            await _webHookSender.SendAsync(notification, webHooks.Where(w => MatchesFilters(w, notification)).ToList());

            return webHooks.Count;
        }

        protected virtual bool MatchesFilters(IWebHook webHook, IWebHookNotification notification)
        {
            return webHook.Filters
                .Where(f => f.ActionId == notification.ActionId)
                .Any(f => f.Parameters.Count == 0 || f.Parameters.All(kvp => notification.Payload.ContainsKey(kvp.Key) && notification.Payload[kvp.Key].Equals(kvp.Value)));
        }
    }
}