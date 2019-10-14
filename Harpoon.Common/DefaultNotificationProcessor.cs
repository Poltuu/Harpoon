using Microsoft.Extensions.Logging;
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
        private readonly ILogger<DefaultNotificationProcessor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultNotificationProcessor"/> class.
        /// </summary>
        /// <param name="webHookStore"></param>
        /// <param name="webHookSender"></param>
        /// <param name="logger"></param>
        public DefaultNotificationProcessor(IWebHookStore webHookStore, IWebHookSender webHookSender, ILogger<DefaultNotificationProcessor> logger)
        {
            _webHookStore = webHookStore ?? throw new ArgumentNullException(nameof(webHookStore));
            _webHookSender = webHookSender ?? throw new ArgumentNullException(nameof(webHookSender));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var tasks = webHooks.Select(w => new { Task = _webHookSender.SendAsync(new WebHookWorkItem(notification, w), cancellationToken), Name = w.Callback });
            try
            {
                await Task.WhenAll(tasks.Select(t => t.Task));
            }
            catch (TaskCanceledException)
            {
                var canceledWebHooks = tasks.Where(a => !a.Task.IsCompleted).Select(a => a.Name);
                _logger.LogError("The following urls have not been called due to a task cancellation: " + string.Join(Environment.NewLine, canceledWebHooks));
            }
            catch
            {
                var canceledWebHooks = tasks.Where(a => !a.Task.IsCompleted).Select(a => a.Name);
                _logger.LogError("The following urls have not been called due to an error: " + string.Join(Environment.NewLine, canceledWebHooks));
            }
        }
    }
}