using Harpoon.Registrations.EFStorage;
using Harpoon.Tests.Mocks;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class WebHookStoreTests
    {
        [Fact]
        public async Task ArgNull()
        {
            Assert.Throws<ArgumentNullException>(() => new WebHookStore<InMemoryContext>(null, new Mock<ISecretProtector>().Object));
            Assert.Throws<ArgumentNullException>(() => new WebHookStore<InMemoryContext>(new InMemoryContext(), null));

            var store = new WebHookStore<InMemoryContext>(new InMemoryContext(), new Mock<ISecretProtector>().Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetApplicableWebHooksAsync(null));
        }

        [Fact]
        public async Task TriggerMatchingTests()
        {
            var notification = new WebHookNotification { TriggerId = "something.interesting.happened" };

            var context = new InMemoryContext();
            context.Add(new WebHook
            {
                IsPaused = false,
                Filters = new List<WebHookFilter> { new WebHookFilter { Trigger = "something.interesting.happened" } }
            });
            context.Add(new WebHook
            {
                IsPaused = true,
                Filters = new List<WebHookFilter> { new WebHookFilter { Trigger = "something.interesting.happened" } }
            });
            context.SaveChanges();

            var secret = "http://www.example.org";
            var protector = new Mock<ISecretProtector>();
            protector.Setup(p => p.Unprotect(It.IsAny<string>())).Returns(secret);

            var store = new WebHookStore<InMemoryContext>(context, protector.Object);

            var result = await store.GetApplicableWebHooksAsync(notification);
            Assert.Equal(1, result.Count);

            var webhook = result[0];
            Assert.False(webhook.IsPaused);

            Assert.Equal(secret, webhook.Secret);
        }
    }
}