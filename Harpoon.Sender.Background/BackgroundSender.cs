using System;
using System.Collections.Generic;
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

        public Task SendAsync(IWebHookNotification notification, IReadOnlyList<IWebHook> webHooks)
        {
            _webHooksQueue.QueueWebHook((notification, webHooks));
            return Task.CompletedTask;
        }
    }
}