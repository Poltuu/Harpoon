using Harpoon.Background;
using Harpoon.Registrations.EFStorage;
using Harpoon.Sender;
using Harpoon.Tests.Mocks;
using MassTransit;
using MassTransit.AspNetCoreIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class MassTransitTests
    {
        class QueuedProcessor<TMessage> : IQueuedProcessor<TMessage>
        {
            public int Counter;

            public Task ProcessAsync(TMessage workItem, CancellationToken token)
            {
                Counter++;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task NotificationSendTests()
        {
            var services = new ServiceCollection();
            services.AddHarpoon(c => c.SendNotificationsUsingMassTransit());

            services.AddMassTransit(p => Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(new Uri("rabbitmq://localhost:5672"), hostConfigurator =>
                {
                    hostConfigurator.Username("guest");
                    hostConfigurator.Password("guest");
                });

                cfg.ConfigureNotificationsConsumer(p, "NotificationsQueue");
            }), x => x.ReceiveNotificationsUsingMassTransit());

            var processor = new QueuedProcessor<IWebHookNotification>();
            services.AddSingleton(typeof(IQueuedProcessor<IWebHookNotification>), processor);

            var provider = services.BuildServiceProvider();

            var token = new CancellationTokenSource();
            await provider.GetRequiredService<IHostedService>().StartAsync(token.Token);

            var service = provider.GetRequiredService<IWebHookService>();

            await service.NotifyAsync(new WebHookNotification
            {
                TriggerId = "trigger",
                Payload = new Dictionary<string, object>()
            });

            await Task.Delay(10000);

            Assert.Equal(1, processor.Counter);
            token.Cancel();
        }

        [Fact]
        public async Task WebHookWorkItemSendTests()
        {
            var services = new ServiceCollection();
            services.AddHarpoon(c => c.SendWebHookWorkItemsUsingMassTransit());

            services.AddMassTransit(p => Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(new Uri("rabbitmq://localhost:5672"), hostConfigurator =>
                {
                    hostConfigurator.Username("guest");
                    hostConfigurator.Password("guest");
                });

                cfg.ConfigureWebHookWorkItemsConsumer(p, "WebHookWorkItemsQueue");
            }), x => x.ReceiveWebHookWorkItemsUsingMassTransit());

            var processor = new QueuedProcessor<IWebHookWorkItem>();
            services.AddSingleton(typeof(IQueuedProcessor<IWebHookWorkItem>), processor);

            var provider = services.BuildServiceProvider();

            var token = new CancellationTokenSource();
            await provider.GetRequiredService<IHostedService>().StartAsync(token.Token);

            var service = provider.GetRequiredService<IWebHookSender>();

            await service.SendAsync(new WebHookWorkItem(new WebHookNotification(), new WebHook()), CancellationToken.None);

            await Task.Delay(10000);

            Assert.Equal(1, processor.Counter);
            token.Cancel();
        }

        [Fact]
        public async Task FullIntegrationMassTransitTests()
        {
            var expectedWebHooksCount = 10;
            var counter = new HttpClientMocker.CounterHandler();
            var services = new ServiceCollection();

            var store = new Mock<IWebHookStore>();
            store.Setup(s => s.GetApplicableWebHooksAsync(It.IsAny<IWebHookNotification>())).ReturnsAsync(() => {
                return Enumerable.Range(0, expectedWebHooksCount).Select(i => new WebHook { Callback = new Uri("http://www.example.org") }).ToList();
            });
            services.AddSingleton(store.Object);

            var protector = new Mock<ISignatureService>();
            protector.Setup(p => p.GetSignature(It.IsAny<string>(), It.IsAny<string>())).Returns("secret");
            services.AddSingleton(protector.Object);

            services.AddHarpoon(c => c.UseAllMassTransitDefaults(a => a.AddHttpMessageHandler(() => counter)));
            services.AddMassTransit(p => Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(new Uri("rabbitmq://localhost:5672"), hostConfigurator =>
                {
                    hostConfigurator.Username("guest");
                    hostConfigurator.Password("guest");
                });

                cfg.ConfigureNotificationsConsumer(p, "NotificationsQueue");
                cfg.ConfigureWebHookWorkItemsConsumer(p, "WebHookWorkItemsQueue");
            }), x => x.UseAllMassTransitDefaults());

            var provider = services.BuildServiceProvider();

            var token = new CancellationTokenSource();
            await provider.GetRequiredService<IHostedService>().StartAsync(token.Token);

            var service = provider.GetRequiredService<IWebHookService>();

            var notif = new WebHookNotification { TriggerId = "noun.verb" };
            await service.NotifyAsync(notif);

            await Task.Delay(10000);

            Assert.Equal(expectedWebHooksCount, counter.Counter);
            token.Cancel();
        }
    }
}