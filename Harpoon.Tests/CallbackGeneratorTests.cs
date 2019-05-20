using Harpoon.Controllers.Swashbuckle;
using Harpoon.Registrations;
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
                ["my.trigger"] = new WebHookTrigger("my.trigger")
                {
                    Description = "blabla",
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema> { ["prop1"] = new OpenApiSchema { Type = "string" } }
                    }
                },
                ["my.trigger.2"] = new WebHookTrigger("my.trigger.2")
                {
                    Description = "blabla2",
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema> { ["prop2"] = new OpenApiSchema { Type = "integer" } }
                    }
                }
            });

            var filter = new WebHookSubscriptionFilter(provider.Object);
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
                ["my.trigger"] = new WebHookTrigger("my.trigger")
                {
                    Description = "blabla",
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema> { ["prop1"] = new OpenApiSchema { Type = "string" } }
                    }
                },
                ["my.trigger.2"] = new WebHookTrigger("my.trigger.2")
                {
                    Description = "blabla2",
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema> { ["prop2"] = new OpenApiSchema { Type = "integer" } }
                    }
                }
            });

            var filter = new WebHookSubscriptionFilter(provider.Object);
            var context = new OperationFilterContext(null, new SchemaGenerator(new SchemaGeneratorOptions(), null), new SchemaRepository(), typeof(CallbackGeneratorTests).GetMethod(nameof(WebHookSubscriptionFilterTests)));
            var operation = new OpenApiOperation();
            filter.Apply(operation, context);

            var json = operation.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
            var yaml = operation.SerializeAsYaml(OpenApiSpecVersion.OpenApi3_0);
        }
    }
}   