using Harpoon.Registrations.EFStorage;
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

            Assert.Throws<ArgumentNullException>(() => new DefaultNotificationProcessor(null, sender.Object));
            Assert.Throws<ArgumentNullException>(() => new DefaultNotificationProcessor(store.Object, null));

            var service = new DefaultNotificationProcessor(store.Object, sender.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.ProcessAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task NoWebHookAsync()
        {
            var store = new Mock<IWebHookStore>();
            store.Setup(s => s.GetAllWebHooksAsync(It.IsAny<string>())).ReturnsAsync(new List<IWebHook>());
            var sender = new Mock<IWebHookSender>();

            var service = new DefaultNotificationProcessor(store.Object, sender.Object);
            await service.ProcessAsync(new WebHookNotification(), CancellationToken.None);

            sender.Verify(s => s.SendAsync(It.IsAny<IWebHookWorkItem>(), It.IsAny<CancellationToken>()), Times.Never);
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

            var service = new DefaultNotificationProcessor(store.Object, sender.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessAsync(new WebHookNotification(), CancellationToken.None));
        }

        [Fact]
        public async Task FilterOnParametersAsync()
        {
            var trigger = "noun.verb";
            var webHooks = new List<WebHook>
            {
                new WebHook
                {
                    Filters = new List<WebHookFilter>
                    {
                        new WebHookFilter
                        {
                            TriggerId = trigger,
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
                            TriggerId = trigger,
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
                            TriggerId = trigger,
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
                            TriggerId = trigger,
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
                            TriggerId = trigger,
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
                            TriggerId = trigger,
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
                TriggerId = trigger,
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

            var service = new DefaultNotificationProcessor(store.Object, sender.Object);
            await service.ProcessAsync(payload, CancellationToken.None);

            sender.Verify(s => s.SendAsync(It.IsAny<IWebHookWorkItem>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }
    }
}