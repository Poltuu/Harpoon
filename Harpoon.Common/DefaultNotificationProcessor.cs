using Harpoon.Background;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon
{
    public class DefaultNotificationProcessor : IQueuedProcessor<IWebHookNotification>, IWebHookService
    {
        private readonly IWebHookStore _webHookStore;
        private readonly IWebHookSender _webHookSender;

        public DefaultNotificationProcessor(IWebHookStore webHookStore, IWebHookSender webHookSender)
        {
            _webHookStore = webHookStore ?? throw new ArgumentNullException(nameof(webHookStore));
            _webHookSender = webHookSender ?? throw new ArgumentNullException(nameof(webHookSender));
        }

        Task IWebHookService.NotifyAsync(IWebHookNotification notification) => ProcessAsync(notification, CancellationToken.None);
        public async Task ProcessAsync(IWebHookNotification notification, CancellationToken token)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var webHooks = await _webHookStore.GetAllWebHooksAsync(notification.TriggerId);
            var workItems = webHooks.Where(w => FiltersMatch(w, notification)).Select(w => new WebHookWorkItem(notification, w));

            await Task.WhenAll(workItems.Select(w => _webHookSender.SendAsync(w, token)));
        }

        protected virtual bool FiltersMatch(IWebHook webHook, IWebHookNotification notification)
        {
            if (webHook.Filters == null)
            {
                throw new InvalidOperationException("WebHook need to expose filters to be considered valid and ready to be send");
            }

            return webHook.Filters
                .Where(f => f.TriggerId == notification.TriggerId)
                .Any(f => f.Parameters == null || f.Parameters.Count == 0 || f.Parameters.All(kvp => notification.Payload.ContainsKey(kvp.Key) && notification.Payload[kvp.Key].Equals(kvp.Value)));
        }
    }
}