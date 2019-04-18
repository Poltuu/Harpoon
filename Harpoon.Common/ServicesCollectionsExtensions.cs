using Harpoon;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServicesCollectionsExtensions
    {
        /// <summary>
        /// Storage and Sender strategy need to be configured.
        /// IWebHookActionProvider needs to be configured if storage is used.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IHarpoonBuilder AddHarpoon(this IServiceCollection services, Action<IHttpClientBuilder> validatorConfiguration)
        {
            services.AddLogging();
            services.TryAddScoped<IWebHookService, DefaultWebHookService>();
            services.TryAddScoped<IWebHookValidator, DefaultWebHookValidator>();

            var builder = services.AddHttpClient<DefaultWebHookValidator>();
            validatorConfiguration?.Invoke(builder);

            return new HarpoonBuilder { Services = services };
        }
    }
}