using Harpoon;
using Harpoon.Registration;
using Harpoon.Registration.EFStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServicesCollectionsExtensions
    {
        /// <summary>
        /// Data protection configuration is required
        /// IWebHookActionProvider needs to be configured.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="harpoon"></param>
        /// <param name="dataProtection"></param>
        /// <returns></returns>
        public static IHarpoonBuilder UseEfStorage<TContext>(this IHarpoonBuilder harpoon, Action<IDataProtectionBuilder> dataProtection)
            where TContext : DbContext, IRegistrationsContext
        {
            if (dataProtection == null)
            {
                throw new ArgumentException("Data protection configuration is required.");
            }

            harpoon.Services.TryAddScoped<IPrincipalIdGetter, DefaultPrincipalIdGetter>();
            harpoon.Services.AddScoped<IWebHookStore, WebHookRegistrationStore<TContext>>();
            harpoon.Services.AddScoped<IWebHookRegistrationStore, WebHookRegistrationStore<TContext>>();

            dataProtection(harpoon.Services.AddDataProtection());

            return harpoon;
        }

        /// <summary>
        /// Data protection configuration is required
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="harpoon"></param>
        /// <param name="dataProtection"></param>
        /// <returns></returns>
        public static IHarpoonBuilder UseEfStorage<TContext, TWebHookActionProvider>(this IHarpoonBuilder harpoon, Action<IDataProtectionBuilder> dataProtection)
            where TContext : DbContext, IRegistrationsContext
            where TWebHookActionProvider : class, IWebHookActionProvider
        {
            harpoon.Services.TryAddScoped<IWebHookActionProvider, TWebHookActionProvider>();
            return harpoon.UseEfStorage<TContext>(dataProtection);
        }
    }
}