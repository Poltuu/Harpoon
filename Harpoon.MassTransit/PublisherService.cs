using MassTransit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.MassTransit
{
    /// <summary>
    /// This class provides an implementation of <see cref="IWebHookService"/> and <see cref="IWebHookSender"/> via event publishing
    /// </summary>
    public class PublisherService : IWebHookService, IWebHookSender
    {
        private readonly IPublishEndpoint _publishEndpoint;

        /// <summary>Initializes a new instance of the <see cref="PublisherService"/> class.</summary>
        public PublisherService(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        }

        /// <inheritdoc />
        Task IWebHookService.NotifyAsync(IWebHookNotification notification, CancellationToken cancellationToken)
            => PublishAsync(notification, cancellationToken);

        /// <inheritdoc />
        Task IWebHookSender.SendAsync(IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
            => PublishAsync(webHookWorkItem, cancellationToken);

        private Task PublishAsync<T>(T message, CancellationToken token) => _publishEndpoint.Publish(message, token);
    }
}