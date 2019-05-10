using Harpoon.Background;
using System;
using System.Threading.Tasks;

namespace Harpoon
{
    public class DefaultWebHookService : IWebHookService
    {
        private readonly BackgroundQueue<IWebHookNotification> _webHooksQueue;

        public DefaultWebHookService(BackgroundQueue<IWebHookNotification> webHooksQueue)
        {
            _webHooksQueue = webHooksQueue ?? throw new ArgumentNullException(nameof(webHooksQueue));
        }

        public Task NotifyAsync(IWebHookNotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            _webHooksQueue.QueueWebHook(notification);
            return Task.CompletedTask;
        }
    }
}