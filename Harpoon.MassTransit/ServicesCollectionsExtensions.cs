using Harpoon;
using Harpoon.MassTransit;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A set of extensions methods on <see cref="IHarpoonBuilder"/> to allow the usage of MassTransit
    /// </summary>
    public static class ServicesCollectionsExtensions
    {
        /// <summary>
        /// Registers every services allowing for a pipeline to treat webhooks via a messaging service.
        /// Some configuration that require a <see cref="IServiceCollectionConfigurator"/> and/or a <see cref="IBusFactoryConfigurator"/> might still be needed.
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IServiceCollection UseAllMassTransitDefaults(this IHarpoonBuilder harpoon) => harpoon.UseAllMassTransitDefaults(b => { });
        /// <summary>
        /// Registers every services allowing for a pipeline to treat webhooks via a messaging service, while allowing sender retry policy configuration
        /// Some configuration that require a <see cref="IServiceCollectionConfigurator"/> and/or a <see cref="IBusFactoryConfigurator"/> might still be needed.
        /// </summary>
        /// <param name="harpoon"></param>
        /// <param name="senderPolicy"></param>
        /// <returns></returns>
        public static IServiceCollection UseAllMassTransitDefaults(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> senderPolicy)
        {
            return harpoon.SendNotificationsUsingMassTransit()
                   .UseDefaultNotificationProcessor()
                   .SendWebHookWorkItemsUsingMassTransit()
                   .UseDefaultWebHookWorkItemProcessor(senderPolicy)
                   .Services;
        }

        /// <summary>
        /// Registers every services allowing for a pipeline to treat webhooks via a messaging service.
        /// Some configuration that require a <see cref="IHarpoonBuilder"/> and/or a <see cref="IBusFactoryConfigurator"/> might still be needed.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static IServiceCollectionConfigurator UseAllMassTransitDefaults(this IServiceCollectionConfigurator x)
        {
            return x.ReceiveNotificationsUsingMassTransit()
                .ReceiveWebHookWorkItemsUsingMassTransit();
        }

        /// <summary>
        /// Registers <see cref="PublisherService"/> as the default <see cref="IWebHookService"/>, allowing for a treatment of <see cref="IWebHookNotification"/> via messaging service
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder SendNotificationsUsingMassTransit(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.AddSingleton<IWebHookService, PublisherService>();
            return harpoon;
        }

        /// <summary>
        /// Registers <see cref="Consumer{IWebHookNotification}"/> as a endpoint receiving <see cref="IWebHookNotification"/>, allowing for receiving and treating <see cref="IWebHookNotification"/> from other applications
        /// </summary>
        /// <param name="x"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IServiceCollectionConfigurator ReceiveNotificationsUsingMassTransit(this IServiceCollectionConfigurator x, Action<IConsumerConfigurator<Consumer<IWebHookNotification>>> configure = null)
        {
            x.AddConsumer(configure);
            return x;
        }

        /// <summary>
        /// Allow for the configuration of the <see cref="Consumer{IWebHookNotification}"/>
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="p"></param>
        /// <param name="queueName"></param>
        /// <param name="configure"></param>
        public static void ConfigureNotificationsConsumer(this IBusFactoryConfigurator cfg, IServiceProvider p, string queueName, Action<IConsumerConfigurator<Consumer<IWebHookNotification>>> configure = null)
        {
            cfg.ReceiveEndpoint(queueName, e => e.ConfigureConsumer(p, configure));
        }

        /// <summary>
        /// Registers <see cref="PublisherService"/> as the default <see cref="IWebHookSender"/>, allowing for a treatment of <see cref="IWebHookWorkItem"/> via messaging service
        /// </summary>
        /// <param name="harpoon"></param>
        /// <returns></returns>
        public static IHarpoonBuilder SendWebHookWorkItemsUsingMassTransit(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.AddSingleton<IWebHookSender, PublisherService>();
            return harpoon;
        }

        /// <summary>
        /// Registers <see cref="Consumer{IWebHookWorkItem}"/> as a endpoint receiving <see cref="IWebHookWorkItem"/>, allowing for receiving and treating <see cref="IWebHookWorkItem"/> from other applications
        /// </summary>
        /// <param name="x"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IServiceCollectionConfigurator ReceiveWebHookWorkItemsUsingMassTransit(this IServiceCollectionConfigurator x, Action<IConsumerConfigurator<Consumer<IWebHookWorkItem>>> configure = null)
        {
            x.AddConsumer(configure);
            return x;
        }

        /// <summary>
        /// Allow for the configuration of the <see cref="Consumer{IWebHookWorkItem}"/>
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="p"></param>
        /// <param name="queueName"></param>
        /// <param name="configure"></param>
        public static void ConfigureWebHookWorkItemsConsumer(this IBusFactoryConfigurator cfg, IServiceProvider p, string queueName, Action<IConsumerConfigurator<Consumer<IWebHookWorkItem>>> configure = null)
        {
            cfg.ReceiveEndpoint(queueName, e => e.ConfigureConsumer(p, configure));
        }
    }
}