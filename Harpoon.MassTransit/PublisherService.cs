using MassTransit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.MassTransit
{
    public class PublisherService : IWebHookService, IWebHookSender
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public PublisherService(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        }

        public Task NotifyAsync(IWebHookNotification notification) => PublishAsync(notification, CancellationToken.None);
        public Task SendAsync(IWebHookWorkItem webHookWorkItem, CancellationToken token) => PublishAsync(webHookWorkItem, token);

        private Task PublishAsync<T>(T message, CancellationToken token) => _publishEndpoint.Publish(message, token);
    }
}