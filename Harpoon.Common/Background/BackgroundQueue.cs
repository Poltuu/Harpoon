using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Harpoon.Background
{
    internal class BackgroundQueue<T>
    {
        private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        public ValueTask QueueWebHookAsync(T workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentException(nameof(workItem));
            }

            return _channel.Writer.WriteAsync(workItem);
        }

        public ValueTask<T> DequeueAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}