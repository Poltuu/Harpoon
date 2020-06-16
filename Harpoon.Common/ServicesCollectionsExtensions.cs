using Harpoon;
using Harpoon.Background;
using Harpoon.Sender;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A set of extensions methods on <see cref="IServiceCollection"/> and <see cref="IHarpoonBuilder"/>
    /// </summary>
    public static class ServicesCollectionsExtensions
    {
        /// <summary>
        /// Adds essential Harpoon services to the specified <see cref="IServiceCollection"/>. Further configuration is required using the <see cref="IHarpoonBuilder"/> from parameter.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IServiceCollection AddHarpoon(this IServiceCollection services, Action<IHarpoonBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.AddLogging();

            var harpoon = new HarpoonBuilder(services);

            configure(harpoon);

            return harpoon.Services;
        }

        /// <summary>
        /// Registers <see cref="DefaultNotificationProcessor"/> as the default <see cref="IQueuedProcessor{IWebHookNotification}"/>.
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder UseDefaultNotificationProcessor(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.TryAddScoped<IQueuedProcessor<IWebHookNotification>, DefaultNotificationProcessor>();
            return harpoon;
        }

        /// <summary>
        /// Registers services to use <see cref="DefaultWebHookSender"/> as the default <see cref="IQueuedProcessor{IWebHookWorkItem}"/>.
        /// To setup your own retry policy, use the second method signature.
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder UseDefaultWebHookWorkItemProcessor(this IHarpoonBuilder harpoon) => harpoon.UseDefaultWebHookWorkItemProcessor(b => { });
        /// <summary>
        /// Registers services to use <see cref="DefaultWebHookSender"/> as the default <see cref="IQueuedProcessor{IWebHookWorkItem}"/>.
        /// </summary>
        /// <param name="harpoon"></param>
        /// <param name="senderPolicy">This parameter lets you define your retry policy</param>
        /// <returns></returns>
        public static IHarpoonBuilder UseDefaultWebHookWorkItemProcessor(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> senderPolicy)
        {
            if (senderPolicy == null)
            {
                throw new ArgumentNullException(nameof(senderPolicy));
            }

            harpoon.Services.TryAddSingleton<ISignatureService, DefaultSignatureService>();

            harpoon.Services.TryAddScoped<IQueuedProcessor<IWebHookWorkItem>, DefaultWebHookSender>();
            var builder = harpoon.Services.AddHttpClient<IQueuedProcessor<IWebHookWorkItem>, DefaultWebHookSender>();
            senderPolicy(builder);
            return harpoon;
        }

        /// <summary>
        /// Registers every services allowing for a synchronous pipeline to treat webhooks locally.
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IServiceCollection UseAllSynchronousDefaults(this IHarpoonBuilder harpoon) => harpoon.UseAllSynchronousDefaults(b => { });
        /// <summary>
        /// Registers every services allowing for a synchronous pipeline to treat webhooks locally, while allowing sender retry policy configuration
        /// </summary>
        /// <param name="harpoon"></param>
        /// <param name="senderPolicy"></param>
        /// <returns></returns>
        public static IServiceCollection UseAllSynchronousDefaults(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> senderPolicy)
            => harpoon.ProcessNotificationsSynchronously()
                .UseDefaultNotificationProcessor()
                .ProcessWebHookWorkItemSynchronously()
                .UseDefaultWebHookWorkItemProcessor(senderPolicy)
                .Services;

        /// <summary>
        /// Registers every services allowing for an asynchronous pipeline to treat webhooks locally, using background services.
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IServiceCollection UseAllLocalDefaults(this IHarpoonBuilder harpoon) => harpoon.UseAllLocalDefaults(b => { });
        /// <summary>
        /// Registers every services allowing for an asynchronous pipeline to treat webhooks locally, using background services, while allowing sender retry policy configuration
        /// </summary>
        /// <param name="harpoon"></param>
        /// <param name="senderPolicy"></param>
        /// <returns></returns>
        public static IServiceCollection UseAllLocalDefaults(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> senderPolicy)
            => harpoon.ProcessNotificationsUsingLocalQueue()
                .UseDefaultNotificationProcessor()
                .ProcessWebHookWorkItemsUsingLocalQueue()
                .UseDefaultWebHookWorkItemProcessor(senderPolicy)
                .Services;

        /// <summary>
        /// Registers <see cref="DefaultNotificationProcessor"/> as the default <see cref="IWebHookService"/>, allowing for a synchronous treatment of <see cref="IWebHookNotification"/>
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder ProcessNotificationsSynchronously(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.TryAddScoped<IWebHookService, DefaultNotificationProcessor>();
            return harpoon;
        }

        /// <summary>
        /// Registers <see cref="IQueuedProcessor{IWebHookWorkItem}"/> as the default <see cref="IWebHookSender"/>, allowing for a synchronous treatment of <see cref="IWebHookWorkItem"/>
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder ProcessWebHookWorkItemSynchronously(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.TryAddScoped(p => p.GetRequiredService<IQueuedProcessor<IWebHookWorkItem>>() as IWebHookSender);
            return harpoon;
        }

        /// <summary>
        /// Registers <see cref="QueuedHostedService{IWebHookNotification}"/> as the default <see cref="IWebHookService"/>, allowing for a background treatment of <see cref="IWebHookNotification"/>
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder ProcessNotificationsUsingLocalQueue(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.TryAddSingleton<IWebHookService, DefaultWebHookService>();
            harpoon.Services.TryAddSingleton<BackgroundQueue<IWebHookNotification>>();
            harpoon.Services.AddHostedService<QueuedHostedService<IWebHookNotification>>();
            return harpoon;
        }

        /// <summary>
        /// Registers <see cref="QueuedHostedService{IWebHookNotification}"/> as the default <see cref="IWebHookSender"/>, allowing for a synchronous treatment of <see cref="IWebHookWorkItem"/>
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder ProcessWebHookWorkItemsUsingLocalQueue(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.TryAddSingleton<IWebHookSender, BackgroundSender>();
            harpoon.Services.TryAddSingleton<BackgroundQueue<IWebHookWorkItem>>();
            harpoon.Services.AddHostedService<QueuedHostedService<IWebHookWorkItem>>();
            return harpoon;
        }
    }
}