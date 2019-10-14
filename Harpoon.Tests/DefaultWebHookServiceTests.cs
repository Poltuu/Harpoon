using Harpoon.Background;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class DefaultWebHookProcessorTests
    {
        [Fact]
        public async Task ArgNullAsync()
        {
            var store = new Mock<IWebHookStore>();
            var sender = new Mock<IWebHookSender>();
            var logger = new Mock<ILogger<DefaultNotificationProcessor>>();

            Assert.Throws<ArgumentNullException>(() => new DefaultNotificationProcessor(null, sender.Object, logger.Object));
            Assert.Throws<ArgumentNullException>(() => new DefaultNotificationProcessor(store.Object, null, logger.Object));
            Assert.Throws<ArgumentNullException>(() => new DefaultNotificationProcessor(store.Object, sender.Object, null));
            Assert.Throws<ArgumentNullException>(() => new DefaultWebHookService(null));

            var service = new DefaultNotificationProcessor(store.Object, sender.Object, logger.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.ProcessAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task DefaultWebHookServiceAsync()
        {
            var queue = new BackgroundQueue<IWebHookNotification>();
            var service = new DefaultWebHookService(queue);
            var count = 0;

            await service.NotifyAsync(new WebHookNotification());
            await queue.DequeueAsync(CancellationToken.None).ContinueWith(t => count++);

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task NoWebHookAsync()
        {
            var store = new Mock<IWebHookStore>();
            store.Setup(s => s.GetApplicableWebHooksAsync(It.IsAny<IWebHookNotification>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<IWebHook>());
            var sender = new Mock<IWebHookSender>();
            var logger = new Mock<ILogger<DefaultNotificationProcessor>>();

            var service = new DefaultNotificationProcessor(store.Object, sender.Object, logger.Object);
            await service.ProcessAsync(new WebHookNotification(), CancellationToken.None);

            sender.Verify(s => s.SendAsync(It.IsAny<IWebHookWorkItem>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}