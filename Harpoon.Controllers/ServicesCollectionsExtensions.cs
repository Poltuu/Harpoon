using Harpoon;
using Harpoon.Controllers.Swashbuckle;
using Harpoon.Registrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.IO;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A set of extensions methods to allow the generation of the harpoon documentation
    /// </summary>
    public static class ServicesCollectionsExtensions
    {
        /// <summary>
        /// Registers necessary services for the controllers
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder AddControllers<TWebHookTriggerProvider>(this IHarpoonBuilder harpoon)
            where TWebHookTriggerProvider : class, IWebHookTriggerProvider
        {
            harpoon.Services.TryAddSingleton<IWebHookTriggerProvider, TWebHookTriggerProvider>();
            return harpoon;
        }

        /// <summary>
        /// Generates a new doc for harpoon webhooks
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static SwaggerGenOptions AddHarpoonDocumentation(this SwaggerGenOptions options)
        {
            options.SwaggerDoc(Harpoon.Controllers.OpenApi.GroupName, new OpenApiInfo
            {
                Title = "WebHooks",
                Description = "WebHook documentation",
                Version = "v1"
            });

            var currentPredicate = options.SwaggerGeneratorOptions.DocInclusionPredicate;
            options.DocInclusionPredicate((documentName, apiDescription) =>
            {
                if (documentName == Harpoon.Controllers.OpenApi.GroupName)
                {
                    return apiDescription.GroupName == documentName;
                }
                return currentPredicate(documentName, apiDescription);
            });

            options.OperationFilter<WebHookSubscriptionFilter>();

            var path = Path.ChangeExtension(typeof(Harpoon.Controllers.OpenApi).Assembly.Location, ".xml");
            if (File.Exists(path))
            {
                options.IncludeXmlComments(path);
            }

            return options;
        }

        /// <summary>
        /// Add an endpoint for the harpoon documentation
        /// </summary>
        /// <param name="options"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static SwaggerUIOptions AddHarpoonEndpoint(this SwaggerUIOptions options, string name = null)
        {
            options.SwaggerEndpoint($"/swagger/{Harpoon.Controllers.OpenApi.GroupName}/swagger.json", name ?? "WebHooks documentation");
            return options;
        }
    }
}