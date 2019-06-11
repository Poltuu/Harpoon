using Harpoon.Registrations;
using Harpoon.Registrations.EFStorage;
using Harpoon.Sender;
using Harpoon.Sender.EF;
using Harpoon.Tests.Fixtures;
using Harpoon.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class WebHookRegistrationStoreTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;

        public WebHookRegistrationStoreTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ArgNull()
        {
            var getter = new Mock<IPrincipalIdGetter>();
            var dataprotection = new Mock<ISecretProtector>();
            var logger = new Mock<ILogger<WebHookRegistrationStore<TestContext1>>>();

            Assert.Throws<ArgumentNullException>(() => new WebHookRegistrationStore<TestContext1>(null, getter.Object, dataprotection.Object, logger.Object));
            Assert.Throws<ArgumentNullException>(() => new WebHookRegistrationStore<TestContext1>(new TestContext1(), null, dataprotection.Object, logger.Object));
            Assert.Throws<ArgumentNullException>(() => new WebHookRegistrationStore<TestContext1>(new TestContext1(), getter.Object, null, logger.Object));
            Assert.Throws<ArgumentNullException>(() => new WebHookRegistrationStore<TestContext1>(new TestContext1(), getter.Object, dataprotection.Object, null));

            var store = _fixture.Provider.GetRequiredService<WebHookRegistrationStore<TestContext1>>();

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.InsertWebHookAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.UpdateWebHookAsync(null, null));
        }

        private async Task SeedAsync(TestContext1 context)
        {
            var triggers = new[] { "noun.verb", "noun.verb2" };
            var pauses = new[] { true, false };
            var principals = new[] { "principal1", "principal2" };

            foreach (var principal in principals)
            {
                foreach (var trigger in triggers)
                {
                    foreach (var pause in pauses)
                    {
                        AddWebHook(context, Guid.NewGuid(), principal, trigger, pause);
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private WebHook AddWebHook(TestContext1 context, Guid id, string principal, string trigger, bool isPaused)
        {
            var result = new WebHook
            {
                Id = id,
                PrincipalId = principal,
                IsPaused = isPaused,
                ProtectedCallback = "aHR0cDovL3d3dy5leGFtcGxlLm9yZw",
                ProtectedSecret = "YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXpBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWjEyMzQ1Njc4OTAtXw",
                Filters = new List<WebHookFilter>
                {
                    new WebHookFilter
                    {
                        Id = Guid.NewGuid(),
                        Trigger = trigger
                    }
                }
            };

            context.Add(result);
            return result;
        }

        private class Payload : Dictionary<string, object>
        {
            public int Property { get; set; }
            public string OtherProperty { get; set; }
            public string ThirdProperty { get; set; }
            public Guid NotificationId { get; set; }
        }

        [Fact]
        public async Task GetAllTests()
        {
            var services = _fixture.Provider;
            var context = services.GetRequiredService<TestContext1>();
            await SeedAsync(context);
            var store = services.GetRequiredService<WebHookStore<TestContext1>>();

            var payload = new WebHookNotification
            {
                TriggerId = "noun.verb",
                Payload = new Payload
                {
                    Property = 2,
                    OtherProperty = "value",
                    ThirdProperty = "nope"
                }
            };
            var webHooks = await store.GetApplicableWebHooksAsync(payload);

            Assert.All(webHooks, t =>
            {
                Assert.NotEqual(Guid.Empty, t.Id);
                Assert.False(t.IsPaused);
                Assert.True(t.Filters != null && t.Filters.Count != 0);
                Assert.Contains("noun.verb", t.Filters.Select(f => f.Trigger));
            });
            AssertHaveBeenPrepared(webHooks);
            Assert.Equal(2, webHooks.Count);
        }

        private void AssertHaveBeenPrepared(IEnumerable<IWebHook> webHooks)
        {
            Assert.All(webHooks, t =>
            {
                Assert.NotNull(t.Filters);
                Assert.Equal("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_", t.Secret);
                Assert.Equal(new Uri("http://www.example.org"), t.Callback);
            });
        }


        [Fact]
        public async Task GetForCurrentUserTests()
        {
            var services = _fixture.Provider;
            var context = services.GetRequiredService<TestContext1>();

            var webHook1 = AddWebHook(context, Guid.NewGuid(), "principal1", "noun.verb", false);
            var webHook2 = AddWebHook(context, Guid.NewGuid(), "principal1", "noun.verb", true);
            var webHook3 = AddWebHook(context, Guid.NewGuid(), "principal2", "noun.verb", false);
            await context.SaveChangesAsync();

            var store = services.GetRequiredService<WebHookRegistrationStore<TestContext1>>();

            var webhook = await store.GetWebHookAsync(null, webHook1.Id);
            Assert.NotNull(webhook);

            var webhook2 = await store.GetWebHookAsync(null, webHook2.Id);
            Assert.NotNull(webhook2);

            var webhook3 = await store.GetWebHookAsync(null, webHook3.Id);
            Assert.Null(webhook3);

            var webhooks = await store.GetWebHooksAsync(null);
            Assert.Equal(2, webhooks.Count);

            AssertHaveBeenPrepared(new[] { webhook, webhook2, webhooks[0], webhooks[1] });
        }

        public static IEnumerable<object[]> InsertScenario => new List<object[]>
        {
            new object[] { new Guid(), new Uri("http://www.example.org"), "secret", new List<WebHookFilter>() },
            new object[] { Guid.NewGuid(), null, "secret", new List<WebHookFilter>() },
            new object[] { Guid.NewGuid(), new Uri("http://www.example.org"), null, new List<WebHookFilter>() },
            new object[] { Guid.NewGuid(), new Uri("http://www.example.org"), "secret", null }
        };

        [Theory]
        [MemberData(nameof(InsertScenario))]
        public async Task InsertArgsTests(Guid id, Uri callback, string secret, List<WebHookFilter> filters)
        {
            var store = _fixture.Provider.GetRequiredService<WebHookRegistrationStore<TestContext1>>();

            var webHook = new WebHook
            {
                Id = id,
                Callback = callback,
                Secret = secret,
                Filters = filters
            };

            await Assert.ThrowsAsync<ArgumentException>(() => store.InsertWebHookAsync(null, webHook));
        }

        [Fact]
        public async Task InsertTests()
        {
            var services = _fixture.Provider;
            var context = services.GetRequiredService<TestContext1>();
            var store = services.GetRequiredService<WebHookRegistrationStore<TestContext1>>();

            var webHook = new WebHook
            {
                Id = Guid.NewGuid(),
                Callback = new Uri("http://www.example.org"),
                Secret = "secret",
                Filters = new List<WebHookFilter> { }
            };

            var initialCount = await context.Set<WebHook>().CountAsync();
            var result = await store.InsertWebHookAsync(null, webHook);
            Assert.Equal(WebHookRegistrationStoreResult.Success, result);

            var dbWebHooks = await context.Set<WebHook>().ToListAsync();
            Assert.Equal(1, dbWebHooks.Count - initialCount);
            Assert.Contains(dbWebHooks, w => w.Id == webHook.Id);
        }

        [Fact]
        public async Task NotFoundTests()
        {
            var store = _fixture.Provider.GetRequiredService<WebHookRegistrationStore<TestContext1>>();

            Assert.Equal(WebHookRegistrationStoreResult.NotFound, await store.UpdateWebHookAsync(null, new WebHook()));
            Assert.Equal(WebHookRegistrationStoreResult.NotFound, await store.DeleteWebHookAsync(null, Guid.NewGuid()));
        }

        public static IEnumerable<object[]> UpdateScenario => new List<object[]>
        {
            new object[] { false, new Uri("http://www.example.org"), "secret",
                new List<WebHookFilter>
                {
                    new WebHookFilter { Trigger = "noun.verb" }
                }
            },
            new object[] { false, new Uri("http://www.example2.org"), "secret",
                new List<WebHookFilter>
                {
                    new WebHookFilter { Trigger = "noun.verb" }
                }
            },
            new object[] { false, new Uri("http://www.example.org"), "secret2",
                new List<WebHookFilter>
                {
                    new WebHookFilter { Trigger = "noun.verb" }
                }
            },
            new object[] { false, new Uri("http://www.example.org"), "secret",
                new List<WebHookFilter>
                {
                    new WebHookFilter { Trigger = "noun.verb" },
                    new WebHookFilter { Trigger = "noun.verb2" }
                }
            },
        };

        [Theory]
        [MemberData(nameof(UpdateScenario))]
        public async Task UpdateTests(bool paused, Uri callback, string secret, List<WebHookFilter> filters)
        {
            var store = _fixture.Provider.GetRequiredService<WebHookRegistrationStore<TestContext1>>();

            var webHook = new WebHook
            {
                Id = Guid.NewGuid(),
                Callback = new Uri("http://www.example.org"),
                Secret = "secret",
                Filters = new List<WebHookFilter>
                {
                    new WebHookFilter { Trigger = "noun.verb" }
                }
            };
            await store.InsertWebHookAsync(null, webHook);

            var newWebHook = new WebHook
            {
                Id = webHook.Id,
                Callback = callback,
                IsPaused = paused,
                Secret = secret,
                Filters = filters
            };

            var result = await store.UpdateWebHookAsync(null, newWebHook);
            Assert.Equal(WebHookRegistrationStoreResult.Success, result);

            var dbWebHook = await store.GetWebHookAsync(null, webHook.Id);
            Assert.Equal(webHook.Id, dbWebHook.Id);
            Assert.Equal(paused, dbWebHook.IsPaused);
            Assert.Equal(callback ?? new Uri("http://www.example.org"), dbWebHook.Callback);
            Assert.Equal(string.IsNullOrEmpty(secret) ? "secret" : secret, dbWebHook.Secret);
            Assert.Equal((filters ?? webHook.Filters).Count, dbWebHook.Filters.Count);
        }

        [Fact]
        public async Task DeleteTestsAsync()
        {
            var store = _fixture.Provider.GetRequiredService<WebHookRegistrationStore<TestContext1>>();

            var webHook = new WebHook
            {
                Id = Guid.NewGuid(),
                Callback = new Uri("http://www.example.org"),
                Secret = "secret",
                Filters = new List<WebHookFilter>
                {
                    new WebHookFilter { Trigger = "noun.verb" }
                }
            };
            await store.InsertWebHookAsync(null, webHook);
            var dbWebHook = await store.GetWebHookAsync(null, webHook.Id);
            Assert.NotNull(dbWebHook);

            var result = await store.DeleteWebHookAsync(null, webHook.Id);
            Assert.Equal(WebHookRegistrationStoreResult.Success, result);

            dbWebHook = await store.GetWebHookAsync(null, webHook.Id);
            Assert.Null(dbWebHook);
        }

        [Fact]
        public async Task DeleteUserTestsAsync()
        {
            var store = _fixture.Provider.GetRequiredService<WebHookRegistrationStore<TestContext1>>();

            //does not throw
            await store.DeleteWebHooksAsync(null);
            await store.DeleteWebHooksAsync(null);

            var webHook = new WebHook
            {
                Id = Guid.NewGuid(),
                Callback = new Uri("http://www.example.org"),
                Secret = "secret",
                Filters = new List<WebHookFilter>
                {
                    new WebHookFilter { Trigger = "noun.verb" }
                }
            };
            await store.InsertWebHookAsync(null, webHook);
            var dbWebHook = await store.GetWebHookAsync(null, webHook.Id);
            Assert.NotNull(dbWebHook);

            await store.DeleteWebHooksAsync(null);
        }

        [Theory]
        [InlineData(System.Net.HttpStatusCode.NotFound)]
        [InlineData(System.Net.HttpStatusCode.Gone)]
        public async Task NotFoundScenarioAsync(System.Net.HttpStatusCode code)
        {
            var logger = new Mock<ILogger<EFWebHookSender<TestContext1>>>();
            var signature = new Mock<ISignatureService>();

            var context = _fixture.Provider.GetRequiredService<TestContext1>();
            var webHook = AddWebHook(context, Guid.NewGuid(), "myPrincipalxxx", "noun.verb", false);
            webHook.Callback = new Uri("http://example.org");
            await context.SaveChangesAsync();
            var notif = new WebHookNotification { TriggerId = "noun.verb" };

            var httpClient = HttpClientMocker.Static(code, "");

            var service = new EFWebHookSender<TestContext1>(httpClient, signature.Object, logger.Object, context);
            await service.SendAsync(new WebHookWorkItem(notif, webHook), CancellationToken.None);

            Assert.True((await context.WebHooks.FirstAsync(w => w.Id == webHook.Id)).IsPaused);
        }
    }
}