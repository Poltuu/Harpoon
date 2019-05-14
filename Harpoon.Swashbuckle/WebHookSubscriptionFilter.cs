using Harpoon.OpenApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace Harpoon.Swashbuckle
{
    /// <summary>
    /// This filter sets operation's callbacks if the givent operation contains the <see cref="WebHookSubscriptionPointAttribute"/>
    /// </summary>
    public class WebHookSubscriptionFilter : IOperationFilter
    {
        private readonly CallbacksGenerator _callbacksGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookSubscriptionFilter"/> class.
        /// </summary>
        /// <param name="callbacksGenerator"></param>
        public WebHookSubscriptionFilter(CallbacksGenerator callbacksGenerator)
        {
            _callbacksGenerator = callbacksGenerator ?? throw new ArgumentNullException(nameof(callbacksGenerator));
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
                operation.Callbacks = _callbacksGenerator.GenerateCallbacks();
            }
        }
    }
}
