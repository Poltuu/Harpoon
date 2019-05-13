using Harpoon;
using Harpoon.MassTransit;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServicesCollectionsExtensions
    {
        public static IServiceCollection UseAllMassTransitDefaults(this IHarpoonBuilder harpoon) => harpoon.UseAllMassTransitDefaults(b => { });
        public static IServiceCollection UseAllMassTransitDefaults(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> senderPolicy)
        {
            return harpoon.SendNotificationsUsingMassTransit()
                   .UseDefaultNotificationProcessor()
                   .SendWebHookWorkItemsUsingMassTransit()
                   .UseDefaultWebHookWorkItemProcessor(senderPolicy)
                   .Services;
        }

        public static IServiceCollectionConfigurator UseAllMassTransitDefaults(this IServiceCollectionConfigurator x)
        {
            return x.ReceiveNotificationsUsingMassTransit()
                .ReceiveWebHookWorkItemsUsingMassTransit();
        }

        public static IHarpoonBuilder SendNotificationsUsingMassTransit(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.AddSingleton<IWebHookService, PublisherService>();
            return harpoon;
        }

        public static IServiceCollectionConfigurator ReceiveNotificationsUsingMassTransit(this IServiceCollectionConfigurator x, Action<IConsumerConfigurator<Consumer<IWebHookNotification>>> configure = null)
        {
            x.AddConsumer(configure);
            return x;
        }

        public static void ConfigureNotificationsConsumer(this IBusFactoryConfigurator cfg, IServiceProvider p, string queueName, Action<IConsumerConfigurator<Consumer<IWebHookNotification>>> configure = null)
        {
            cfg.ReceiveEndpoint(queueName, e => e.ConfigureConsumer(p, configure));
        }

        public static IHarpoonBuilder SendWebHookWorkItemsUsingMassTransit(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.AddSingleton<IWebHookSender, PublisherService>();
            return harpoon;
        }

        public static IServiceCollectionConfigurator ReceiveWebHookWorkItemsUsingMassTransit(this IServiceCollectionConfigurator x, Action<IConsumerConfigurator<Consumer<IWebHookWorkItem>>> configure = null)
        {
            x.AddConsumer(configure);
            return x;
        }

        public static void ConfigureWebHookWorkItemsConsumer(this IBusFactoryConfigurator cfg, IServiceProvider p, string queueName, Action<IConsumerConfigurator<Consumer<IWebHookWorkItem>>> configure = null)
        {
            cfg.ReceiveEndpoint(queueName, e => e.ConfigureConsumer(p, configure));
        }
    }
}