using Harpoon;
using Harpoon.Registrations;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A set of extensions methods on <see cref="IHarpoonBuilder"/>
    /// </summary>
    public static class ServicesCollectionsExtensions
    {   
        /// <summary>
        /// Registers services to use the default <see cref="IWebHookValidator"/> implementation. Necessary to use default controllers.
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder UseDefaultValidator(this IHarpoonBuilder harpoon) => harpoon.UseDefaultValidator(b => { });
        /// <summary>
        /// Registers services to use the default <see cref="IWebHookValidator"/> implementation. Necessary to use default controllers.
        /// </summary>
        /// <param name="harpoon"></param>
        /// <param name="validatorPolicy"></param>
        /// <returns></returns>
        public static IHarpoonBuilder UseDefaultValidator(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> validatorPolicy)
        {
            if (validatorPolicy == null)
            {
                throw new ArgumentNullException(nameof(validatorPolicy));
            }

            harpoon.Services.TryAddScoped<IWebHookValidator, DefaultWebHookValidator>();

            var builder = harpoon.Services.AddHttpClient<IWebHookValidator, DefaultWebHookValidator>();
            validatorPolicy(builder);
            return harpoon;
        }
    }
}
