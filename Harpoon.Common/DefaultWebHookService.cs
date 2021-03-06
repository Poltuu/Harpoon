﻿using Harpoon.Background;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon
{
    internal class DefaultWebHookService : IWebHookService
    {
        private readonly BackgroundQueue<IWebHookNotification> _webHooksQueue;

        public DefaultWebHookService(BackgroundQueue<IWebHookNotification> webHooksQueue)
        {
            _webHooksQueue = webHooksQueue ?? throw new ArgumentNullException(nameof(webHooksQueue));
        }

        public async Task NotifyAsync(IWebHookNotification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            await _webHooksQueue.QueueWebHookAsync(notification);
        }
    }
}