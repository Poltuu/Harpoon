using Harpoon.Background;
using Harpoon.Registrations.EFStorage;
using Harpoon.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System;
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
            var service = new BackgroundSender(new BackgroundQueue<IWebHookWorkItem>());
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task NormalAsync()
        {
            var queue = new BackgroundQueue<IWebHookWorkItem>();
            var service = new BackgroundSender(queue);
            var count = 0;

            await service.SendAsync(new WebHookWorkItem(Guid.NewGuid(), new WebHookNotification("", new object()), new WebHook()), CancellationToken.None);
            await queue.DequeueAsync(CancellationToken.None).AsTask().ContinueWith(t => count++);

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task NormalWithHostedServiceAsync()
        {
            var service = new BackgroundSender(_fixture.Services.GetRequiredService<BackgroundQueue<IWebHookWorkItem>>());
            await service.SendAsync(new WebHookWorkItem(Guid.NewGuid(), new WebHookNotification("", new object()), new WebHook()), CancellationToken.None);

            await Task.Delay(10000);
            Assert.Equal(1, BackgroundSenderFixture.FakeWebHookSenderCount);
        }
    }
}