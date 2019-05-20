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

            protected override Task OnFailureAsync(Exception exception, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
            {
                Failures += 1;
                return base.OnFailureAsync(exception, webHookWorkItem, cancellationToken);
            }

            protected override Task OnNotFoundAsync(IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
            {
                NotFounds += 1;
                return base.OnNotFoundAsync(webHookWorkItem, cancellationToken);
            }

            protected override Task OnSuccessAsync(IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
            {
                Successes += 1;
                return base.OnSuccessAsync(webHookWorkItem, cancellationToken);
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

        public static IEnumerable<object[]> PayloadData => new List<object[]>
        {
            new object[] { null },
            new object[] { new Payloadable { NotificationId = Guid.NewGuid()} },
        };

        [Theory]
        [MemberData(nameof(PayloadData))]
        public async Task NormalScenarioAsync(IPayloadable payload)
        {
            var logger = new Mock<ILogger<DefaultWebHookSender>>();
            var signature = "FIXED_SIGNATURE";
            var signatureService = new Mock<ISignatureService>();
            signatureService.Setup(s => s.GetSignature(It.IsAny<string>(), It.IsAny<string>())).Returns(signature);

            var webHook = new WebHook { Callback = new Uri("http://www.example.com") };
            var notif = new WebHookNotification { TriggerId = "noun.verb", Payload = payload };

            var callbackHasBeenCalled = false;
            var httpClient = HttpClientMocker.Callback(async m =>
            {
                callbackHasBeenCalled = true;
                Assert.Equal(HttpMethod.Post, m.Method);
                Assert.Equal(webHook.Callback, m.RequestUri);

                var content = JsonConvert.DeserializeObject<Payloadable>(await m.Content.ReadAsStringAsync());

                var headers = m.Headers.Select(kvp => kvp.Key).ToHashSet();
                Assert.Contains(DefaultWebHookSender.TimestampKey, headers);
                Assert.Contains(DefaultWebHookSender.UniqueIdKey, headers);

                if (notif.Payload != null)
                {
                    Assert.NotEqual(default, content.NotificationId);
                    Assert.Equal(content.NotificationId.ToString(), m.Headers.GetValues(DefaultWebHookSender.UniqueIdKey).First());
                }

                Assert.Contains(DefaultWebHookSender.TriggerKey, headers);
                Assert.Equal(notif.TriggerId, m.Headers.GetValues(DefaultWebHookSender.TriggerKey).First());

                Assert.Contains(DefaultWebHookSender.SignatureHeader, headers);
                Assert.Equal(signature, m.Headers.GetValues(DefaultWebHookSender.SignatureHeader).First());
            });

            var service = new CounterDefaultWebHookSender(httpClient, signatureService.Object, logger.Object);
            await service.SendAsync(new WebHookWorkItem(notif, webHook), CancellationToken.None);

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

            var webHook = new WebHook { Callback = new Uri("http://www.example.com") };
            var notif = new WebHookNotification { TriggerId = "noun.verb" };

            var httpClient = HttpClientMocker.Static(code, "");

            var service = new CounterDefaultWebHookSender(httpClient, signature.Object, logger.Object);
            await service.SendAsync(new WebHookWorkItem(notif, webHook), CancellationToken.None);

            Assert.Equal(1, service.NotFounds);
        }

        [Fact]
        public async Task ErrorScenarioAsync()
        {
            var logger = new Mock<ILogger<DefaultWebHookSender>>();
            var signature = new Mock<ISignatureService>();

            var webHook = new WebHook { Callback = new Uri("http://www.example.com") };
            var notif = new WebHookNotification { TriggerId = "noun.verb" };

            var httpClient = HttpClientMocker.AlwaysFail(new Exception());

            var service = new CounterDefaultWebHookSender(httpClient, signature.Object, logger.Object);
            await service.SendAsync(new WebHookWorkItem(notif, webHook), CancellationToken.None);

            Assert.Equal(1, service.Failures);
        }
    }
}