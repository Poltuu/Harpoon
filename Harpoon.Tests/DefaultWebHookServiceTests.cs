using Harpoon.Registration.EFStorage;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class DefaultWebHookServiceTests
    {
        [Fact]
        public async Task ArgNullAsync()
        {
            var store = new Mock<IWebHookStore>();
            var sender = new Mock<IWebHookSender>();

            Assert.Throws<ArgumentNullException>(() => new DefaultWebHookService(null, sender.Object));
            Assert.Throws<ArgumentNullException>(() => new DefaultWebHookService(store.Object, null));

            var service = new DefaultWebHookService(store.Object, sender.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.NotifyAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task NoWebHookAsync()
        {
            var store = new Mock<IWebHookStore>();
            store.Setup(s => s.GetAllWebHooksAsync(It.IsAny<string>())).ReturnsAsync(new List<IWebHook>());
            var sender = new Mock<IWebHookSender>();

            var service = new DefaultWebHookService(store.Object, sender.Object);
            var result = await service.NotifyAsync(new WebHookNotification(), CancellationToken.None);

            Assert.Equal(0, result);
            sender.Verify(s => s.SendAsync(It.IsAny<IWebHookNotification>(), It.IsAny<IReadOnlyList<IWebHook>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task EmptyFilterFailsAsync()
        {
            var webHooks = new List<WebHook>
            {
                new WebHook
                {
                    Filters = null
                },
            };

            var store = new Mock<IWebHookStore>();
            store.Setup(s => s.GetAllWebHooksAsync(It.IsAny<string>())).ReturnsAsync(webHooks);
            var sender = new Mock<IWebHookSender>();

            var service = new DefaultWebHookService(store.Object, sender.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.NotifyAsync(new WebHookNotification(), CancellationToken.None));
        }

        [Fact]
        public async Task FilterOnParametersAsync()
        {
            var actionId = "myAction";
            var webHooks = new List<WebHook>
            {
                new WebHook
                {
                    Filters = new List<WebHookFilter>
                    {
                        new WebHookFilter
                        {
                            ActionId = actionId,
                            Parameters = null, //valid
                        }
                    }
                },
                new WebHook
                {
                    Filters = new List<WebHookFilter>
                    {
                        new WebHookFilter
                        {
                            ActionId = actionId,
                            Parameters = new Dictionary<string, object>(), //valid
                        }
                    }
                },
                new WebHook
                {
                    Filters = new List<WebHookFilter>
                    {
                        new WebHookFilter
                        {
                            ActionId = actionId,
                            Parameters = new Dictionary<string, object> //valid
                            {
                                ["property"] = 2,
                                ["otherProperty"] = "value"
                            }
                        }
                    }
                },
                new WebHook
                {
                    Filters = new List<WebHookFilter>
                    {
                        new WebHookFilter
                        {
                            ActionId = actionId,
                            Parameters = new Dictionary<string, object>
                            {
                                ["property"] = 2,
                                ["otherProperty"] = "value",
                                ["absentProperty"] = "absent" //not valid
                            }
                        }
                    }
                },
                new WebHook
                {
                    Filters = new List<WebHookFilter>
                    {
                        new WebHookFilter
                        {
                            ActionId = actionId,
                            Parameters = new Dictionary<string, object>
                            {
                                ["property"] = 20, //not valid
                                ["otherProperty"] = "value"
                            }
                        }
                    }
                },
                new WebHook
                {
                    Filters = new List<WebHookFilter>
                    {
                        new WebHookFilter
                        {
                            ActionId = actionId,
                            Parameters = new Dictionary<string, object>
                            {
                                ["property"] = 20,
                                ["otherProperty"] = "wrongValue" //not valid
                            }
                        }
                    }
                }
            };
            var payload = new WebHookNotification
            {
                ActionId = actionId,
                Payload = new Dictionary<string, object>
                {
                    ["property"] = 2,
                    ["otherProperty"] = "value",
                    ["thirdProperty"] = "nope"
                }
            };

            var store = new Mock<IWebHookStore>();
            store.Setup(s => s.GetAllWebHooksAsync(It.IsAny<string>())).ReturnsAsync(webHooks);
            var sender = new Mock<IWebHookSender>();

            var service = new DefaultWebHookService(store.Object, sender.Object);
            var result = await service.NotifyAsync(payload, CancellationToken.None);

            Assert.Equal(3, result);
            sender.Verify(s => s.SendAsync(It.IsAny<IWebHookNotification>(), It.IsAny<IReadOnlyList<IWebHook>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}