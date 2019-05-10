using Harpoon.Background;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Sender
{
    public class BackgroundSender : IWebHookSender
    {
        private readonly BackgroundQueue<IWebHookWorkItem> _webHooksQueue;

        public BackgroundSender(BackgroundQueue<IWebHookWorkItem> webHooksQueue)
        {
            _webHooksQueue = webHooksQueue ?? throw new ArgumentNullException(nameof(webHooksQueue));
        }

        public Task SendAsync(IWebHookWorkItem webHookWorkItem, CancellationToken token)
        {
            if (webHookWorkItem == null)
            {
                throw new ArgumentNullException(nameof(webHookWorkItem));
            }

            _webHooksQueue.QueueWebHook(webHookWorkItem);
            return Task.CompletedTask;
        }
    }
}