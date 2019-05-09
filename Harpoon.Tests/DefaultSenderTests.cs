using Harpoon.Registrations.EFStorage;
using Harpoon.Sender;
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

            protected override Task OnFailureAsync(Exception exception, IWebHookNotification notification, IWebHook webHook)
            {
                Failures += 1;
                return base.OnFailureAsync(exception, notification, webHook);
            }

            protected override Task OnNotFoundAsync(IWebHookNotification notification, IWebHook webHook)
            {
                NotFounds += 1;
                return base.OnNotFoundAsync(notification, webHook);
            }

            protected override Task OnSuccessAsync(IWebHookNotification notification, IWebHook webHook)
            {
                Successes += 1;
                return base.OnSuccessAsync(notification, webHook);
            }
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
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendAsync(null, new List<IWebHook>(), CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendAsync(new WebHookNotification(), null, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendAsync(new WebHookNotification(), new List<IWebHook> { null }, CancellationToken.None));

            //empty scenario is also tested here
            await service.SendAsync(new WebHookNotification(), new List<IWebHook> { }, CancellationToken.None);
        }

        public static IEnumerable<object[]> PayloadData => new List<object[]>
        {
            new object[] { null },
            new object[] { new Dictionary<string,object>() },
            new object[] { new Dictionary<string, object> { ["param1"] = "value1" } },
        };

        [Theory]
        [MemberData(nameof(PayloadData))]
        public async Task NormalScenarioAsync(Dictionary<string, object> payload)
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

                var content = JsonConvert.DeserializeObject<Dictionary<string, string>>(await m.Content.ReadAsStringAsync());
                Assert.NotNull(content);
                Assert.Contains(DefaultWebHookSender.TriggerKey, content.Keys);
                Assert.Equal(notif.TriggerId, content[DefaultWebHookSender.TriggerKey]);
                Assert.Contains(DefaultWebHookSender.TimestampKey, content.Keys);
                Assert.Contains(DefaultWebHookSender.UniqueIdKey, content.Keys);

                if (notif.Payload != null)
                {
                    foreach (var kvp in notif.Payload)
                    {
                        Assert.Contains(kvp.Key, content.Keys);
                        Assert.Equal(kvp.Value, content[kvp.Key]);
                    }
                }

                Assert.Contains(DefaultWebHookSender.SignatureHeader, m.Headers.Select(kvp => kvp.Key));
                Assert.Equal(signature, m.Headers.GetValues(DefaultWebHookSender.SignatureHeader).First());
            });

            var service = new CounterDefaultWebHookSender(httpClient, signatureService.Object, logger.Object);
            await service.SendAsync(notif, new List<IWebHook> { webHook }, CancellationToken.None);

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
            await service.SendAsync(notif, new List<IWebHook> { webHook }, CancellationToken.None);

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
            await service.SendAsync(notif, new List<IWebHook> { webHook }, CancellationToken.None);

            Assert.Equal(1, service.Failures);
        }
    }
}