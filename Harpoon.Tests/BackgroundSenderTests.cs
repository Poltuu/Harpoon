using Harpoon.Registrations.EFStorage;
using Harpoon.Sender.Background;
using Harpoon.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class BackgroundSenderTests : IClassFixture<BackgroundSenderFixture>
    {
        private readonly BackgroundSenderFixture _fixture;

        public BackgroundSenderTests(BackgroundSenderFixture fixture)
        {
            _fixture = fixture;
        }

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

        [Fact]
        public async Task NormalWithHostedServiceAsync()
        {
            var service = new BackgroundSender(_fixture.Services.GetRequiredService<WebHooksQueue>());
            await service.SendAsync(new WebHookNotification(), new List<IWebHook> { new WebHook() }, CancellationToken.None);

            await Task.Delay(10000);
            Assert.Equal(1, BackgroundSenderFixture.FakeWebHookSenderCount);
        }
    }
}