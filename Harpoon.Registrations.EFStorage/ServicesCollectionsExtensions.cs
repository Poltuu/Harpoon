using Harpoon;
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
    /// <summary>
    /// A set of extensions methods on <see cref="IHarpoonBuilder"/> to allow the usage of EF Core
    /// </summary>
    public static class ServicesCollectionsExtensions
    {
        /// <summary>
        /// Registers 
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TWebHookTriggerProvider"></typeparam>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder RegisterWebHooksUsingEfStorage<TContext, TWebHookTriggerProvider>(this IHarpoonBuilder harpoon)
            where TContext : DbContext, IRegistrationsContext
            where TWebHookTriggerProvider : class, IWebHookTriggerProvider
        {
            harpoon.Services.TryAddScoped<IWebHookTriggerProvider, TWebHookTriggerProvider>();
            return harpoon.RegisterWebHooksUsingEfStorage<TContext>();
        }

        /// <summary>
        /// Registers <see cref="WebHookStore{TContext}"/> as <see cref="IWebHookStore"/> and <see cref="WebHookRegistrationStore{TContext}"/> as <see cref="IWebHookRegistrationStore"/>.
        /// TWebHookTriggerProvider needs to be configured.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder RegisterWebHooksUsingEfStorage<TContext>(this IHarpoonBuilder harpoon)
            where TContext : DbContext, IRegistrationsContext
        {
            harpoon.Services.TryAddScoped<IPrincipalIdGetter, DefaultPrincipalIdGetter>();
            harpoon.Services.TryAddSingleton<IWebHookMatcher, DefaultWebHookMatcher>();
            harpoon.Services.TryAddScoped<IWebHookStore, WebHookStore<TContext>>();
            harpoon.Services.TryAddScoped<IWebHookRegistrationStore, WebHookRegistrationStore<TContext>>();

            return harpoon;
        }

        /// <summary>
        /// Registers <see cref="DefaultSecretProtector"/> as <see cref="ISecretProtector"/>
        /// </summary>
        /// <param name="harpoon"></param>
        /// <param name="dataProtection"></param>
        /// <param name="setupAction"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Registers services to use <see cref="EFWebHookSender{TContext}"/> as the default <see cref="IQueuedProcessor{IWebHookWorkItem}"/>.
        /// To setup your own retry policy, use the second method signature.
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder UseDefaultEFWebHookWorkItemProcessor<TContext>(this IHarpoonBuilder harpoon)
            where TContext : DbContext, IRegistrationsContext
            => harpoon.UseDefaultEFWebHookWorkItemProcessor<TContext>(b => { });

        /// <summary>
        /// Registers services to use <see cref="EFWebHookSender{TContext}"/> as the default <see cref="IQueuedProcessor{IWebHookWorkItem}"/>.
        /// </summary>
        /// <param name="harpoon"></param>
        /// <param name="senderPolicy">This parameter lets you define your retry policy</param>
        /// <returns></returns>
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