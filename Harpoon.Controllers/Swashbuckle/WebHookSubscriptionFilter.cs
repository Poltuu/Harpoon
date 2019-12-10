using Harpoon.Registrations;
using Harpoon.Sender;
using Microsoft.OpenApi.Expressions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Harpoon.Controllers.Swashbuckle
{
    /// <summary>
    /// This filter sets operation's callbacks if the given operation contains the <see cref="WebHookSubscriptionPointAttribute"/>
    /// </summary>
    public class WebHookSubscriptionFilter : IOperationFilter
    {
        private static readonly OpenApiResponses _responses = new OpenApiResponses
        {
            ["200"] = new OpenApiResponse { Description = "Your server must returns this code if it accepts the callback." },
            ["404"] = new OpenApiResponse { Description = "If your server returns this code, the webhook might be paused." },
            ["410"] = new OpenApiResponse { Description = "If your server returns this code, the webhook might be paused." },
            ["500"] = new OpenApiResponse { Description = "If your server returns this code, the webhook will go through the error pipeline." },
        };

        private static readonly List<OpenApiParameter> _parameters = new List<OpenApiParameter>
        {
            new OpenApiParameter
            {
                In = ParameterLocation.Header,
                Name = DefaultWebHookSender.SignatureHeader,
                Schema = new OpenApiSchema { Type = "string", Format = "HMACSHA256" }
            },
            new OpenApiParameter
            {
                In = ParameterLocation.Header,
                Name = DefaultWebHookSender.TimestampKey,
                Schema = new OpenApiSchema { Type = "string", Format = "date-time" }
            },
            new OpenApiParameter
            {
                In = ParameterLocation.Header,
                Name = DefaultWebHookSender.TriggerKey,
                Schema = new OpenApiSchema { Type = "string" }
            },
            new OpenApiParameter
            {
                In = ParameterLocation.Header,
                Name = DefaultWebHookSender.UniqueIdKey,
                Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
            },
        };

        private readonly IWebHookTriggerProvider _webHookTriggerProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookSubscriptionFilter"/> class.
        /// </summary>
        /// <param name="webHookTriggerProvider"></param>
        public WebHookSubscriptionFilter(IWebHookTriggerProvider webHookTriggerProvider)
        {
            _webHookTriggerProvider = webHookTriggerProvider ?? throw new ArgumentNullException(nameof(webHookTriggerProvider));
        }

        /// <summary>
        /// Sets operation's callbacks if the operation contains the <see cref="WebHookSubscriptionPointAttribute"/>
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.MethodInfo.GetCustomAttributes(true).OfType<WebHookSubscriptionPointAttribute>().Any())
            {
                operation.Callbacks = _webHookTriggerProvider.GetAvailableTriggers().ToDictionary(t => t.Key, t => Generate(t.Value, context));
            }
        }

        /// <summary>
        /// Generate a single <see cref="OpenApiCallback"/> matching the given <see cref="WebHookTrigger"/>
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        protected OpenApiCallback Generate(WebHookTrigger trigger, OperationFilterContext context)
        {
            var schema = context.SchemaGenerator.GenerateSchema(trigger.PayloadType, context.SchemaRepository);
            var result = new OpenApiCallback();

            result.AddPathItem(RuntimeExpression.Build($"{{$request.body#/{PseudoCamelCase(nameof(IWebHook.Callback))}}}"), new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Post] = new OpenApiOperation
                    {
                        OperationId = trigger.Id,
                        Description = trigger.Description,
                        Responses = _responses,
                        Parameters = _parameters,
                        RequestBody = new OpenApiRequestBody
                        {
                            Required = true,
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = schema
                                }
                            }
                        },
                    }
                }
            });

            return result;
        }

        private string PseudoCamelCase(string input) => char.ToLowerInvariant(input[0]) + input.Substring(1);
    }
}