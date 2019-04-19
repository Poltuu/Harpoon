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
        public static IHarpoonBuilder AddHarpoon(this IServiceCollection services)
            => services.AddHarpoon(b => { });

        /// <summary>
        /// Storage and Sender strategy need to be configured.
        /// IWebHookActionProvider needs to be configured if storage is used.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IHarpoonBuilder AddHarpoon(this IServiceCollection services, Action<IHttpClientBuilder> validatorConfiguration)
        {
            if (validatorConfiguration == null)
            {
                throw new ArgumentNullException(nameof(validatorConfiguration));
            }

            services.AddLogging();
            services.TryAddScoped<IWebHookService, DefaultWebHookService>();
            services.TryAddScoped<IWebHookValidator, DefaultWebHookValidator>();

            validatorConfiguration(services.AddHttpClient<IWebHookValidator, DefaultWebHookValidator>());

            return new HarpoonBuilder { Services = services };
        }
    }
}