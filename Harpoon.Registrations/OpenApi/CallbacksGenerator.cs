using Harpoon.Sender;
using Microsoft.OpenApi.Expressions;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Harpoon.Registrations.OpenApi
{
    /// <summary>
    /// A class able to generate the OpenApiCallbacks field matching the injected <see cref="IWebHookTriggerProvider"/>
    /// </summary>
    public class CallbacksGenerator
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
        /// Initializes a new instance of the <see cref="CallbacksGenerator"/> class.
        /// </summary>
        /// <param name="webHookTriggerProvider"></param>
        public CallbacksGenerator(IWebHookTriggerProvider webHookTriggerProvider)
        {
            _webHookTriggerProvider = webHookTriggerProvider ?? throw new ArgumentNullException(nameof(webHookTriggerProvider));
        }

        /// <summary>
        /// Generates the appropriate <see cref="OpenApiCallback"/> fields matching the given <see cref="IWebHookTriggerProvider"/>
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, OpenApiCallback> GenerateCallbacks()
            => _webHookTriggerProvider.GetAvailableTriggers().ToDictionary(t => t.Key, t => Generate(t.Value));

        /// <summary>
        /// Generate a single <see cref="OpenApiCallback"/> matching the given <see cref="WebHookTrigger"/>
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        protected OpenApiCallback Generate(WebHookTrigger trigger)
        {
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
                            Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new OpenApiMediaType { Schema = trigger.Template } }
                        },
                    }
                }
            });

            return result;
        }

        private string PseudoCamelCase(string input) => char.ToLowerInvariant(input[0]) + input.Substring(1);
    }
}