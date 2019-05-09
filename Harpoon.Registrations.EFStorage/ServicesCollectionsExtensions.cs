using Harpoon;
using Harpoon.Registrations;
using Harpoon.Registrations.EFStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServicesCollectionsExtensions
    {
        /// <summary>
        /// Data protection configuration is required. String parameter is the Purpose or created IDataProtector.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="harpoon"></param>
        /// <param name="dataProtection"></param>
        /// <returns></returns>
        public static IHarpoonBuilder UseEfStorage<TContext, TWebHookTriggerProvider>(this IHarpoonBuilder harpoon, Action<string, IDataProtectionBuilder> dataProtection)
            where TContext : DbContext, IRegistrationsContext
            where TWebHookTriggerProvider : class, IWebHookTriggerProvider
        {
            harpoon.Services.TryAddScoped<IWebHookTriggerProvider, TWebHookTriggerProvider>();
            return harpoon.UseEfStorage<TContext>(dataProtection);
        }

        /// <summary>
        /// Data protection configuration is required. String parameter is the Purpose or created IDataProtector.
        /// TWebHookTriggerProvider needs to be configured.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="harpoon"></param>
        /// <param name="dataProtection"></param>
        /// <returns></returns>
        public static IHarpoonBuilder UseEfStorage<TContext>(this IHarpoonBuilder harpoon, Action<string, IDataProtectionBuilder> dataProtection)
            where TContext : DbContext, IRegistrationsContext
            => harpoon.UseEfStorage<TContext>(dataProtection, o => { });

        /// <summary>
        /// Data protection configuration is required. String parameter is the Purpose or created IDataProtector.
        /// TWebHookTriggerProvider needs to be configured.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="harpoon"></param>
        /// <param name="dataProtection"></param>
        /// <returns></returns>
        public static IHarpoonBuilder UseEfStorage<TContext>(this IHarpoonBuilder harpoon, Action<string, IDataProtectionBuilder> dataProtection, Action<DataProtectionOptions> setupAction)
            where TContext : DbContext, IRegistrationsContext
        {
            if (dataProtection == null)
            {
                throw new ArgumentNullException("Data protection configuration is required.", nameof(dataProtection));
            }

            harpoon.Services.TryAddScoped<IPrincipalIdGetter, DefaultPrincipalIdGetter>();
            harpoon.Services.AddScoped<IWebHookStore, WebHookRegistrationStore<TContext>>();
            harpoon.Services.AddScoped<IWebHookRegistrationStore, WebHookRegistrationStore<TContext>>();

            dataProtection(DataProtection.Purpose, harpoon.Services.AddDataProtection(setupAction));

            return harpoon;
        }
    }
}