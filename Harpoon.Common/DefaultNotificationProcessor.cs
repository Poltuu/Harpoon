using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon
{
    /// <summary>
    /// Default <see cref="IQueuedProcessor{IWebHookNotification}"/> implementation
    /// </summary>
    public class DefaultNotificationProcessor : IQueuedProcessor<IWebHookNotification>, IWebHookService
    {
        private readonly IWebHookStore _webHookStore;
        private readonly IWebHookSender _webHookSender;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultNotificationProcessor"/> class.
        /// </summary>
        /// <param name="webHookStore"></param>
        /// <param name="webHookSender"></param>
        public DefaultNotificationProcessor(IWebHookStore webHookStore, IWebHookSender webHookSender)
        {
            _webHookStore = webHookStore ?? throw new ArgumentNullException(nameof(webHookStore));
            _webHookSender = webHookSender ?? throw new ArgumentNullException(nameof(webHookSender));
        }

        Task IWebHookService.NotifyAsync(IWebHookNotification notification, CancellationToken cancellationToken)
            => ProcessAsync(notification, cancellationToken);

        /// <inheritdoc />
        public async Task ProcessAsync(IWebHookNotification notification, CancellationToken cancellationToken)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var webHooks = await _webHookStore.GetApplicableWebHooksAsync(notification, cancellationToken);

            await Task.WhenAll(webHooks.Select(w => _webHookSender.SendAsync(new WebHookWorkItem(notification, w), cancellationToken)));
        }
    }
}