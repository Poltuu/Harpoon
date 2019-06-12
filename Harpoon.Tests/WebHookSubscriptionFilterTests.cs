using Harpoon.Controllers.Swashbuckle;
using Harpoon.Registrations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Harpoon.Tests
{
    public class WebHookSubscriptionFilterTests
    {
        class MyPayload
        {
            public Guid NotificationId { get; set; }
        }

        class TestWebHookTriggerProvider : IWebHookTriggerProvider
        {
            public IReadOnlyDictionary<string, WebHookTrigger> GetAvailableTriggers()
                => new Dictionary<string, WebHookTrigger>
                {
                    ["trigger"] = new WebHookTrigger<MyPayload>("trigger")
                    {
                        Description = "decription"
                    }
                };
        }

        [Fact]
        public void ArgNull() => Assert.Throws<ArgumentNullException>(() => new WebHookSubscriptionFilter(null));

        [Fact]
        public void ApplyTests()
        {
            var filter = new WebHookSubscriptionFilter(new TestWebHookTriggerProvider());

            var context = new OperationFilterContext(null, new SchemaGenerator((SchemaGeneratorOptions)null, null), new SchemaRepository(), typeof(TestedController).GetMethod(nameof(TestedController.TestedMethod)));
            var operation = new OpenApiOperation();
            filter.Apply(operation, context);

            Assert.NotNull(operation.Callbacks);
            Assert.NotEqual(0, operation.Callbacks.Count);
            Assert.Contains("trigger", operation.Callbacks.Keys);
            Assert.Contains("{$request.body#/callback}", operation.Callbacks["trigger"].PathItems.Keys.Select(k => k.Expression));
            Assert.Contains(OperationType.Post, operation.Callbacks["trigger"].PathItems.First().Value.Operations.Keys);

            var op = operation.Callbacks["trigger"].PathItems.First().Value.Operations[OperationType.Post];

            Assert.Equal("trigger", op.OperationId);
            Assert.Equal("decription", op.Description);
            Assert.Equal(4, op.Parameters.Count);
            Assert.Equal(4, op.Responses.Count);
        }
    }

    class TestedController
    {
        [WebHookSubscriptionPoint]
        public void TestedMethod()
        {

        }
    }
}