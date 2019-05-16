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
            var store = new WebHookStore<InMemoryContext>(new InMemoryContext(), new Mock<ISecretProtector>().Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetApplicableWebHooksAsync(null));
        }

        [Theory]
        [InlineData(null, 0)]
        [InlineData("", 0)]
        [InlineData("something", 0)]
        [InlineData("something.interesting.happened", 1)]
        public async Task TriggerMatchingTests(string registered, int expected)
        {
            var notification = new WebHookNotification { TriggerId = "something.interesting.happened" };

            var context = new InMemoryContext();
            context.Add(new WebHook
            {
                IsPaused = false,
                Filters = new List<WebHookFilter>
                {
                    new WebHookFilter
                    {
                        Trigger = registered
                    }
                }
            });
            context.SaveChanges();
            var protector = new Mock<ISecretProtector>();
            protector.Setup(p => p.Unprotect(It.IsAny<string>())).Returns("http://www.example.org");
            var store = new WebHookStore<InMemoryContext>(context, protector.Object);

            var result = await store.GetApplicableWebHooksAsync(notification);
            Assert.Equal(expected, result.Count);
        }
    }
}