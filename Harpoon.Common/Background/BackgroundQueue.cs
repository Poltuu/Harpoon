using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Background
{
    internal class BackgroundQueue<T>
    {
        private readonly ConcurrentQueue<T> _workItems = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void QueueWebHook(T workItem)
        {
            if (workItem == default)
            {
                throw new ArgumentException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        public async Task<T> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }
    }
}