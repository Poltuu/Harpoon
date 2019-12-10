using MassTransit;
using System;
using System.Threading.Tasks;

namespace Harpoon.MassTransit
{
    /// <summary>
    /// Default message consumer
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public class Consumer<TMessage> : IConsumer<TMessage>
         where TMessage : class
    {
        private readonly IQueuedProcessor<TMessage> _processor;

        /// <summary>Initializes a new instance of the <see cref="Consumer{TMessage}"/> class.</summary>
        public Consumer(IQueuedProcessor<TMessage> processor)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        }

        /// <inheritdoc />
        public Task Consume(ConsumeContext<TMessage> context)
            => _processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}