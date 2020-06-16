using Harpoon.Registrations.EFStorage;
using Harpoon.Sender;
using Harpoon.Sender.EF;
using Harpoon.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class EFNotificationProcessorTests
    {
        [Fact]
        public void ArgNullEfProcessor()
        {
            var store = new Mock<IWebHookStore>();
            var sender = new Mock<IWebHookSender>();
            var logger = new Mock<ILogger<EFNotificationProcessor<TestContext1>>>();
            Assert.Throws<ArgumentNullException>(() => new EFNotificationProcessor<TestContext1>(null, store.Object, sender.Object, logger.Object));
        }
        public class MyPayload
        {
            public Guid NotificationId { get; set; }
            public int Property { get; set; }
        }

        [Fact]
        public async Task EfProcessorLogsAsync()
        {
            var store = new Mock<IWebHookStore>();
            store.Setup(s => s.GetApplicableWebHooksAsync(It.IsAny<IWebHookNotification>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<IWebHook> { new WebHook { } });
            var sender = new Mock<IWebHookSender>();
            var logger = new Mock<ILogger<EFNotificationProcessor<InMemoryContext>>>();
            var context = new InMemoryContext();
            var processor = new EFNotificationProcessor<InMemoryContext>(context, store.Object, sender.Object, logger.Object);

            await processor.ProcessAsync(new WebHookNotification("trigger", new MyPayload { NotificationId = Guid.NewGuid(), Property = 23 }), CancellationToken.None);

            Assert.Single(context.WebHookNotifications);
        }
    }

    public class DefaultSenderTests
    {
        class CounterDefaultWebHookSender : DefaultWebHookSender
        {
            public int Failures { get; private set; }
            public int NotFounds { get; private set; }
            public int Successes { get; private set; }

            public CounterDefaultWebHookSender(HttpClient httpClient, ISignatureService signatureService, ILogger<DefaultWebHookSender> logger)
                : base(httpClient, signatureService, logger)
            {
            }

            protected override Task OnFailureAsync(HttpResponseMessage response, Exception exception, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
            {
                Failures += 1;
                return Task.CompletedTask;
            }

            protected override Task OnNotFoundAsync(HttpResponseMessage response, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
            {
                NotFounds += 1;
                return Task.CompletedTask;
            }

            protected override Task OnSuccessAsync(HttpResponseMessage response, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
            {
                Successes += 1;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void ArgNullEfSender()
        {
            var logger = new Mock<ILogger<EFWebHookSender<TestContext1>>>();
            var httpClient = HttpClientMocker.Static(System.Net.HttpStatusCode.OK, "");
            var signature = new Mock<ISignatureService>();
            Assert.Throws<ArgumentNullException>(() => new EFWebHookSender<TestContext1>(httpClient, signature.Object, logger.Object, null));
        }

        [Fact]
        public async Task ArgNullAsync()
        {
            var logger = new Mock<ILogger<DefaultWebHookSender>>();
            var httpClient = HttpClientMocker.Static(System.Net.HttpStatusCode.OK, "");
            var signature = new Mock<ISignatureService>();

            Assert.Throws<ArgumentNullException>(() => new DefaultWebHookSender(null, signature.Object, logger.Object));
            Assert.Throws<ArgumentNullException>(() => new DefaultWebHookSender(httpClient, null, logger.Object));
            Assert.Throws<ArgumentNullException>(() => new DefaultWebHookSender(httpClient, signature.Object, null));

            var service = new DefaultWebHookSender(httpClient, signature.Object, logger.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendAsync(null, CancellationToken.None));
        }

        public class MyPayload
        {
            public Guid NotificationId { get; set; }
            public int Property { get; set; }
        }
        public static IEnumerable<object[]> PayloadData => new List<object[]>
        {
            new object[] { null },
            new object[] { new Dictionary<string, object>
            {
                ["NotificationId"] = Guid.NewGuid(),
                ["Property"] = 23
            } },
            new object[] { new MyPayload
            {
                NotificationId = Guid.NewGuid(),
                Property = 23
            } },
        };

        [Theory]
        [MemberData(nameof(PayloadData))]
        public async Task NormalScenarioAsync(object payload)
        {
            var logger = new Mock<ILogger<DefaultWebHookSender>>();
            var signature = "FIXED_SIGNATURE";
            var signatureService = new Mock<ISignatureService>();
            signatureService.Setup(s => s.GetSignature(It.IsAny<string>(), It.IsAny<string>())).Returns(signature);

            var webHook = new WebHook { Callback = "http://www.example.com" };
            var notif = new WebHookNotification("noun.verb",  payload);

            var callbackHasBeenCalled = false;
            var httpClient = HttpClientMocker.Callback(async m =>
            {
                callbackHasBeenCalled = true;
                Assert.Equal(HttpMethod.Post, m.Method);
                Assert.Equal(new Uri(webHook.Callback), m.RequestUri);

                var content = JsonConvert.DeserializeObject<Dictionary<string, object>>(await m.Content.ReadAsStringAsync());

                var headers = m.Headers.Select(kvp => kvp.Key).ToHashSet();
                Assert.Contains(DefaultWebHookSender.TimestampKey, headers);
                Assert.Contains(DefaultWebHookSender.UniqueIdKey, headers);

                if (notif.Payload != null)
                {
                    Assert.NotEqual(default, content["notificationId"]);
                    Assert.Equal("23", content["property"].ToString());
                }

                Assert.Contains(DefaultWebHookSender.TriggerKey, headers);
                Assert.Equal(notif.TriggerId, m.Headers.GetValues(DefaultWebHookSender.TriggerKey).First());

                Assert.Contains(DefaultWebHookSender.SignatureHeader, headers);
                Assert.Equal(signature, m.Headers.GetValues(DefaultWebHookSender.SignatureHeader).First());
            });

            var service = new CounterDefaultWebHookSender(httpClient, signatureService.Object, logger.Object);
            await service.SendAsync(new WebHookWorkItem(Guid.NewGuid(), notif, webHook), CancellationToken.None);

            Assert.True(callbackHasBeenCalled);
            Assert.Equal(1, service.Successes);
        }

        [Theory]
        [InlineData(System.Net.HttpStatusCode.NotFound)]
        [InlineData(System.Net.HttpStatusCode.Gone)]
        public async Task NotFoundScenarioAsync(System.Net.HttpStatusCode code)
        {
            var logger = new Mock<ILogger<DefaultWebHookSender>>();
            var signature = new Mock<ISignatureService>();

            var webHook = new WebHook { Callback = "http://www.example.com" };
            var notif = new WebHookNotification("noun.verb", new object());

            var httpClient = HttpClientMocker.Static(code, "");

            var service = new CounterDefaultWebHookSender(httpClient, signature.Object, logger.Object);
            await service.SendAsync(new WebHookWorkItem(Guid.NewGuid(), notif, webHook), CancellationToken.None);

            Assert.Equal(1, service.NotFounds);
        }

        [Fact]
        public async Task ErrorScenarioAsync()
        {
            var logger = new Mock<ILogger<DefaultWebHookSender>>();
            var signature = new Mock<ISignatureService>();

            var webHook = new WebHook { Callback = "http://www.example.com" };
            var notif = new WebHookNotification("noun.verb", new object());

            var httpClient = HttpClientMocker.AlwaysFail(new Exception());

            var service = new CounterDefaultWebHookSender(httpClient, signature.Object, logger.Object);
            await service.SendAsync(new WebHookWorkItem(Guid.NewGuid(), notif, webHook), CancellationToken.None);

            Assert.Equal(1, service.Failures);
        }
    }
}