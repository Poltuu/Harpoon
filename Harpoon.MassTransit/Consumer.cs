using Harpoon.Background;
using MassTransit;
using System;
using System.Threading.Tasks;

namespace Harpoon.MassTransit
{
    public class Consumer<TMessage> : IConsumer<TMessage>
         where TMessage : class
    {
        private readonly IQueuedProcessor<TMessage> _processor;

        public Consumer(IQueuedProcessor<TMessage> processor)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        }

        public async Task Consume(ConsumeContext<TMessage> context)
        {
            await _processor.ProcessAsync(context.Message, context.CancellationToken);
        }
    }
}