using Harpoon.Controllers;
using Harpoon.Controllers.Swashbuckle;
using Harpoon.Registrations;
using Harpoon.Registrations.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Swashbuckle.AspNetCore.SwaggerGen
{
    /// <summary>
    /// A set of extensions methods on <see cref="SwaggerGenOptions"/> to allow the generation of the harpoon documentation
    /// </summary>
    public static class SwaggerGenOptionsExtensions
    {
        /// <summary>
        /// Generates a new doc for harpoon webhooks
        /// </summary>
        /// <typeparam name="TWebHookTriggerProvider"></typeparam>
        /// <param name="options"></param>
        /// <param name="services"></param>
        /// <returns></returns>
        public static SwaggerGenOptions AddHarpoonDocumentation<TWebHookTriggerProvider>(this SwaggerGenOptions options, IServiceCollection services)
            where TWebHookTriggerProvider : class, IWebHookTriggerProvider
        {
            services.TryAddSingleton<IWebHookTriggerProvider, TWebHookTriggerProvider>();
            options.AddHarpoonDocumentation(services);
            return options;
        }

        /// <summary>
        /// Generates a new doc for harpoon webhooks
        /// <see cref="IWebHookTriggerProvider"/> needs to be registered as well
        /// </summary>
        /// <param name="options"></param>
        /// <param name="services"></param>
        /// <returns></returns>
        public static SwaggerGenOptions AddHarpoonDocumentation(this SwaggerGenOptions options, IServiceCollection services)
        {
            options.SwaggerDoc(OpenApi.GroupName, new OpenApiInfo
            {
                Title = "WebHooks",
                Description = "WebHook documentation",
                Version = "v1"
            });
            options.OperationFilter<WebHookSubscriptionFilter>();

            services.TryAddSingleton<CallbacksGenerator>();
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
            options.SwaggerEndpoint($"/swagger/{OpenApi.GroupName}/swagger.json", name ?? "WebHooks documentation");
            return options;
        }
    }
}