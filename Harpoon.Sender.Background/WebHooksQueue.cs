using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Sender.Background
{
    public class WebHooksQueue
    {
        private readonly ConcurrentQueue<(IWebHookNotification, IReadOnlyList<IWebHook>)> _workItems
            = new ConcurrentQueue<(IWebHookNotification, IReadOnlyList<IWebHook>)>();

        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void QueueWebHook((IWebHookNotification, IReadOnlyList<IWebHook>) webHook)
        {
            if (webHook == default)
            {
                throw new ArgumentException(nameof(webHook));
            }

            _workItems.Enqueue(webHook);
            _signal.Release();
        }

        public async Task<(IWebHookNotification, IReadOnlyList<IWebHook>)> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }
    }
}