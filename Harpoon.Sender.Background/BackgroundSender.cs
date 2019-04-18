using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Sender.Background
{
    public class BackgroundSender : IWebHookSender
    {
        private readonly WebHooksQueue _webHooksQueue;

        public BackgroundSender(WebHooksQueue webHooksQueue)
        {
            _webHooksQueue = webHooksQueue ?? throw new ArgumentNullException(nameof(webHooksQueue));
        }

        public Task SendAsync(IWebHookNotification notification, IReadOnlyList<IWebHook> webHooks, CancellationToken token)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (webHooks == null)
            {
                throw new ArgumentNullException(nameof(webHooks));
            }

            if (webHooks.Count == 0)
            {
                return Task.CompletedTask;
            }

            _webHooksQueue.QueueWebHook((notification, webHooks));
            return Task.CompletedTask;
        }
    }
}