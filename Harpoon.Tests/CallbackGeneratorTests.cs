using Harpoon.OpenApi;
using Harpoon.Swashbuckle;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using Xunit;

namespace Harpoon.Tests
{
    public class CallbackGeneratorTests
    {
        [Fact]
        public void ArgNull()
        {
            Assert.Throws<ArgumentNullException>(() => new WebHookSubscriptionFilter(null));
        }

        [Fact]
        public void WebHookSubscriptionFilterTests()
        {
            var provider = new Mock<IWebHookTriggerProvider>();
            provider.Setup(p => p.GetAvailableTriggers()).Returns(new Dictionary<string, WebHookTrigger>
            {
                ["my.trigger"] = new WebHookTrigger
                {
                    Description = "blabla",
                    Id = "my.trigger",
                    Template = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema> { ["prop1"] = new OpenApiSchema { Type = "string" } }
                    }
                },
                ["my.trigger.2"] = new WebHookTrigger
                {
                    Description = "blabla2",
                    Id = "my.trigger.2",
                    Template = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema> { ["prop2"] = new OpenApiSchema { Type = "integer" } }
                    }
                }
            });

            var filter = new WebHookSubscriptionFilter(new CallbacksGenerator(provider.Object));
            var context = new OperationFilterContext(null, null, null, typeof(CallbackGeneratorTests).GetMethod(nameof(WebHookSubscriptionFilterTests)));
            var operation = new OpenApiOperation();
            filter.Apply(operation, context);

            Assert.NotNull(operation.Callbacks);
        }

        [Fact]
        public void GenerationDoesNotThrow()
        {
            var provider = new Mock<IWebHookTriggerProvider>();
            provider.Setup(p => p.GetAvailableTriggers()).Returns(new Dictionary<string, WebHookTrigger>
            {
                ["my.trigger"] = new WebHookTrigger
                {
                    Description = "blabla",
                    Id = "my.trigger",
                    Template = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema> { ["prop1"] = new OpenApiSchema { Type = "string" } }
                    }
                },
                ["my.trigger.2"] = new WebHookTrigger
                {
                    Description = "blabla2",
                    Id = "my.trigger.2",
                    Template = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema> { ["prop2"] = new OpenApiSchema { Type = "integer" } }
                    }
                }
            });

            var generator = new CallbacksGenerator(provider.Object);
            var result = generator.GenerateCallbacks();

            var operation = new OpenApiOperation
            {
                Callbacks = result
            };

            var json = operation.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
            var yaml = operation.SerializeAsYaml(OpenApiSpecVersion.OpenApi3_0);
        }
    }
}   