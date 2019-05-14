using Harpoon.Registrations.EFStorage;
using Harpoon.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class DefaultWebHookValidatorTests
    {
        [Fact]
        public async Task ArgNullAsync()
        {
            var triggers = new Mock<IWebHookTriggerProvider>();
            var logger = new Mock<ILogger<DefaultWebHookValidator>>();
            var client = new Mock<HttpClient>();

            Assert.Throws<ArgumentNullException>(() => new DefaultWebHookValidator(null, logger.Object, client.Object));
            Assert.Throws<ArgumentNullException>(() => new DefaultWebHookValidator(triggers.Object, null, client.Object));
            Assert.Throws<ArgumentNullException>(() => new DefaultWebHookValidator(triggers.Object, logger.Object, null));

            var service = new DefaultWebHookValidator(triggers.Object, logger.Object, client.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.ValidateAsync(null));
        }

        [Fact]
        public async Task InvalidCasesAsync()
        {
            var schema = new OpenApiSchema
            {
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["param1"] = new OpenApiSchema { Type = "integer" },
                    ["param2"] = new OpenApiSchema { Type = "string" },
                    ["param3"] = new OpenApiSchema { Type = "array", Items = new OpenApiSchema { Type = "integer" } },
                    ["param4"] = new OpenApiSchema { Type = "array", Reference = new OpenApiReference() },
                    ["number"] = new OpenApiSchema { Type = "number" },
                    ["integer"] = new OpenApiSchema { Type = "integer" },
                    ["bool"] = new OpenApiSchema { Type = "boolean" },
                    ["object"] = new OpenApiSchema { Type = "object" },
                }
            };
            var triggersById = new Dictionary<string, WebHookTrigger>
            {
                ["valid"] = new WebHookTrigger { Id = "valid", Template = schema },
            };
            var triggers = new Mock<IWebHookTriggerProvider>();
            triggers.Setup(s => s.GetAvailableTriggers()).Returns(triggersById);
            var logger = new Mock<ILogger<DefaultWebHookValidator>>();
            var client = HttpClientMocker.Static(System.Net.HttpStatusCode.NotFound, "fail");
            var service = new DefaultWebHookValidator(triggers.Object, logger.Object, client);

            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Secret = "tooshort" }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Secret = "toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_toolong_" }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook()));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter>() }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "invalid" } } }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid", Parameters = new Dictionary<string, object> { [""] = "" } } } }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid", Parameters = new Dictionary<string, object> { ["invalid"] = "" } } } }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid", Parameters = new Dictionary<string, object> { ["param3"] = "" } } } }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid", Parameters = new Dictionary<string, object> { ["param4.next"] = "" } } } }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid", Parameters = new Dictionary<string, object> { ["param1"] = "string" } } } }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid", Parameters = new Dictionary<string, object> { ["param1"] = null } } } }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid", Parameters = new Dictionary<string, object> { ["number"] = true } } } }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid", Parameters = new Dictionary<string, object> { ["integer"] = 2.3 } } } }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid", Parameters = new Dictionary<string, object> { ["boolean"] = 345 } } } }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid", Parameters = new Dictionary<string, object> { ["object"] = 23 } } } }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid" } } }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid" } }, Callback = new Uri("c:/data") }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid" } }, Callback = new Uri("ftp://data") }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid" } }, Callback = new Uri("http://www.example.com") }));

            service = new DefaultWebHookValidator(triggers.Object, logger.Object, HttpClientMocker.AlwaysFail(new Exception()));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateAsync(new WebHook { Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid" } }, Callback = new Uri("http://www.example.com") }));
        }

        public static IEnumerable<object[]> ValidCasesData => new List<object[]>
        {
            new object[] { new WebHook {
                Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid" } },
                Callback = new Uri("http://www.example.com?noecho")
            } },
            new object[] { new WebHook {
                Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid" } },
                Callback = new Uri("https://www.example.com?noecho")
            } },
            new object[] { new WebHook {
                Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid" } },
                Callback = new Uri("http://www.example.com")
            } },
            new object[] { new WebHook {
                Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid", Parameters = new Dictionary<string, object> { } } },
                Callback = new Uri("https://www.example.com")
            } },
            new object[] { new WebHook {
                Id = Guid.NewGuid(),
                Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid" } },
                Callback = new Uri("http://www.example.com")
            } },
            new object[] { new WebHook {
                Id = Guid.NewGuid(),
                Secret = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_",
                Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid" } },
                Callback = new Uri("https://www.example.com")
            } },
            new object[] { new WebHook {
                Id = Guid.NewGuid(),
                Secret = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_",
                Filters = new List<WebHookFilter> { new WebHookFilter { TriggerId = "valid", Parameters = new Dictionary<string, object> { { "param2", "value" } } } },
                Callback = new Uri("http://www.example.com")
            } },
        };

        [Theory]
        [MemberData(nameof(ValidCasesData))]
        public async Task ValidCasesAsync(WebHook validWebHook)
        {
            var schema = new OpenApiSchema
            {
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["param1"] = new OpenApiSchema { Type = "integer" },
                    ["param2"] = new OpenApiSchema { Type = "string" },
                }
            };
            var triggersById = new Dictionary<string, WebHookTrigger>
            {
                ["valid"] = new WebHookTrigger { Id = "valid", Template = schema },
            };

            var triggers = new Mock<IWebHookTriggerProvider>();
            triggers.Setup(s => s.GetAvailableTriggers()).Returns(triggersById);
            var logger = new Mock<ILogger<DefaultWebHookValidator>>();
            var client = HttpClientMocker.ReturnQueryParam("echo");
            var service = new DefaultWebHookValidator(triggers.Object, logger.Object, client);

            await service.ValidateAsync(validWebHook);

            Assert.NotEqual(default, validWebHook.Id);
            Assert.Equal(64, validWebHook.Secret.Length);
            Assert.NotNull(validWebHook.Filters);
            Assert.NotEmpty(validWebHook.Filters);
            foreach (var filter in validWebHook.Filters)
            {
                Assert.True(triggersById.ContainsKey(filter.TriggerId));
                if (filter.Parameters != null)
                {
                    foreach (var key in filter.Parameters.Keys)
                    {
                        Assert.Contains(key, triggersById[filter.TriggerId].Template.Properties.Keys);
                    }
                }
            }
            Assert.NotNull(validWebHook.Callback);
            Assert.True(validWebHook.Callback.IsAbsoluteUri && validWebHook.Callback.ToString().ToLowerInvariant().StartsWith("http"));
        }
    }
}