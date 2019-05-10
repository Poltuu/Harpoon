using Harpoon;
using Harpoon.Background;
using Harpoon.Registrations;
using Harpoon.Registrations.EFStorage;
using Harpoon.Sender;
using Harpoon.Sender.EF;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServicesCollectionsExtensions
    {
        public static IHarpoonBuilder RegisterWebHooksUsingEfStorage<TContext, TWebHookTriggerProvider>(this IHarpoonBuilder harpoon)
            where TContext : DbContext, IRegistrationsContext
            where TWebHookTriggerProvider : class, IWebHookTriggerProvider
        {
            harpoon.Services.TryAddScoped<IWebHookTriggerProvider, TWebHookTriggerProvider>();
            return harpoon.RegisterWebHooksUsingEfStorage<TContext>();
        }

        /// <summary>
        /// TWebHookTriggerProvider needs to be configured.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="harpoon"></param>
        /// <param name="dataProtection"></param>
        /// <returns></returns>
        public static IHarpoonBuilder RegisterWebHooksUsingEfStorage<TContext>(this IHarpoonBuilder harpoon)
            where TContext : DbContext, IRegistrationsContext
        {
            harpoon.Services.TryAddScoped<IPrincipalIdGetter, DefaultPrincipalIdGetter>();
            harpoon.Services.AddScoped<IWebHookStore, WebHookStore<TContext>>();
            harpoon.Services.AddScoped<IWebHookRegistrationStore, WebHookRegistrationStore<TContext>>();

            return harpoon;
        }

        public static IHarpoonBuilder UseDefaultDataProtection(this IHarpoonBuilder harpoon, Action<IDataProtectionBuilder> dataProtection, Action<DataProtectionOptions> setupAction)
        {
            if (dataProtection == null)
            {
                throw new ArgumentNullException("Data protection configuration is required.", nameof(dataProtection));
            }

            harpoon.Services.TryAddScoped<ISecretProtector, DefaultSecretProtector>();
            dataProtection(harpoon.Services.AddDataProtection(setupAction));
            return harpoon;
        }

        public static IHarpoonBuilder UseDefaultEFWebHookWorkItemProcessor<TContext>(this IHarpoonBuilder harpoon)
            where TContext : DbContext, IRegistrationsContext
            => harpoon.UseDefaultEFWebHookWorkItemProcessor<TContext>(b => { });

        public static IHarpoonBuilder UseDefaultEFWebHookWorkItemProcessor<TContext>(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> senderPolicy)
            where TContext : DbContext, IRegistrationsContext
        {
            if (senderPolicy == null)
            {
                throw new ArgumentNullException(nameof(senderPolicy));
            }

            harpoon.Services.TryAddSingleton<ISignatureService, DefaultSignatureService>();
            harpoon.Services.TryAddScoped<IQueuedProcessor<IWebHookWorkItem>, EFWebHookSender<TContext>>();
            var builder = harpoon.Services.AddHttpClient<IQueuedProcessor<IWebHookWorkItem>, EFWebHookSender<TContext>>();
            senderPolicy(builder);
            return harpoon;
        }
    }
}