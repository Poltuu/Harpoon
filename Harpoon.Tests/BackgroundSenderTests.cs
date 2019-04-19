using Harpoon.Registrations.EFStorage;
using Harpoon.Sender.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class BackgroundSenderTests
    {
        [Fact]
        public async Task ArgNullAsync()
        {
            Assert.Throws<ArgumentNullException>(() => new BackgroundSender(null));
            var service = new BackgroundSender(new WebHooksQueue());
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendAsync(null, new List<IWebHook>(), CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendAsync(new WebHookNotification(), null, CancellationToken.None));
        }

        [Fact]
        public async Task EmptyAsync()
        {
            var queue = new WebHooksQueue();
            var service = new BackgroundSender(queue);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queue.DequeueAsync(CancellationToken.None).ContinueWith(t => throw new Exception("semaphore has somehow been released"));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            await service.SendAsync(new WebHookNotification(), new List<IWebHook> { }, CancellationToken.None);
            await Task.Delay(2);
        }

        [Fact]
        public async Task NormalAsync()
        {
            var queue = new WebHooksQueue();
            var service = new BackgroundSender(queue);
            var count = 0;

            await service.SendAsync(new WebHookNotification(), new List<IWebHook> { new WebHook() }, CancellationToken.None);
            await queue.DequeueAsync(CancellationToken.None).ContinueWith(t => count++);

            Assert.Equal(1, count);
        }

        public class FakeWebHookSender : IWebHookSender
        {
            public static int Count { get; private set; }
            public Task SendAsync(IWebHookNotification notification, IReadOnlyList<IWebHook> webHooks, CancellationToken token)
            {
                Count++;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task NormalWithHostedServiceAsync()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new Mock<ILogger<QueuedHostedService<FakeWebHookSender>>>().Object);
            services.AddScoped<FakeWebHookSender>();
            services.AddHostedService<QueuedHostedService<FakeWebHookSender>>();
            services.AddSingleton<WebHooksQueue>();

            var provider = services.BuildServiceProvider();
            var backGroundService = provider.GetRequiredService<IHostedService>() as QueuedHostedService<FakeWebHookSender>;

            var service = new BackgroundSender(provider.GetRequiredService<WebHooksQueue>());
            await backGroundService.StartAsync(CancellationToken.None);

            await service.SendAsync(new WebHookNotification(), new List<IWebHook> { new WebHook() }, CancellationToken.None);
            await Task.Delay(10);
            Assert.Equal(1, FakeWebHookSender.Count);

            await backGroundService.StopAsync(CancellationToken.None);
        }

        public class FailerWebHookSender : IWebHookSender
        {
            public static int Count { get; private set; }
            public Task SendAsync(IWebHookNotification notification, IReadOnlyList<IWebHook> webHooks, CancellationToken token)
            {
                Count++;
                throw new Exception("");
            }
        }

        [Fact]
        public async Task FailWithHostedServiceAsync()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new Mock<ILogger<QueuedHostedService<FailerWebHookSender>>>().Object);
            services.AddScoped<FailerWebHookSender>();
            services.AddHostedService<QueuedHostedService<FailerWebHookSender>>();
            services.AddSingleton<WebHooksQueue>();

            var provider = services.BuildServiceProvider();
            var backGroundService = provider.GetRequiredService<IHostedService>() as QueuedHostedService<FailerWebHookSender>;

            var service = new BackgroundSender(provider.GetRequiredService<WebHooksQueue>());
            await backGroundService.StartAsync(CancellationToken.None);

            await service.SendAsync(new WebHookNotification(), new List<IWebHook> { new WebHook() }, CancellationToken.None);
            await Task.Delay(10);
            Assert.Equal(1, FailerWebHookSender.Count);

            await backGroundService.StopAsync(CancellationToken.None);
            //no exception thrown is success here
        }
    }
}