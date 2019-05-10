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

            var webHooks = await _webHookStore.GetApplicableWebHooksAsync(notification);

            await Task.WhenAll(webHooks.Select(w => _webHookSender.SendAsync(new WebHookWorkItem(notification, w), token)));
        }
    }
}