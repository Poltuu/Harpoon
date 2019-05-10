using Harpoon;
using Harpoon.Background;
using Harpoon.Sender;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServicesCollectionsExtensions
    {
        public static IServiceCollection AddHarpoon(this IServiceCollection services, Action<IHarpoonBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.AddLogging();

            var harpoon = new HarpoonBuilder { Services = services };

            configure(harpoon);

            return harpoon.Services;
        }

        public static IHarpoonBuilder UseDefaultNotificationProcessor(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.TryAddScoped<IQueuedProcessor<IWebHookNotification>, DefaultNotificationProcessor>();
            return harpoon;
        }

        public static IHarpoonBuilder UseDefaultWebHookWorkItemProcessor(this IHarpoonBuilder harpoon) => harpoon.UseDefaultWebHookWorkItemProcessor(b => { });
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

        public static IHarpoonBuilder UseDefaultValidator(this IHarpoonBuilder harpoon) => harpoon.UseDefaultValidator(b => { });
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

        public static IServiceCollection UseAllSynchronousDefaults(this IHarpoonBuilder harpoon) => harpoon.UseAllSynchronousDefaults(b => { });
        public static IServiceCollection UseAllSynchronousDefaults(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> senderPolicy)
            => harpoon.ProcessNotificationsSynchronously()
                .UseDefaultNotificationProcessor()
                .ProcessWebHookWorkItemSynchronously()
                .UseDefaultWebHookWorkItemProcessor(senderPolicy)
                .Services;

        public static IServiceCollection UseAllLocalDefaults(this IHarpoonBuilder harpoon) => harpoon.UseAllLocalDefaults(b => { });
        public static IServiceCollection UseAllLocalDefaults(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> senderPolicy)
            => harpoon.ProcessNotificationsUsingLocalQueue()
                .UseDefaultNotificationProcessor()
                .ProcessWebHookWorkItemsUsingLocalQueue()
                .UseDefaultWebHookWorkItemProcessor(senderPolicy)
                .Services;

        public static IHarpoonBuilder ProcessNotificationsSynchronously(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.TryAddScoped<IWebHookService, DefaultNotificationProcessor>();
            return harpoon;
        }

        public static IHarpoonBuilder ProcessWebHookWorkItemSynchronously(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.TryAddScoped<IWebHookSender>(p => p.GetRequiredService<IQueuedProcessor<IWebHookWorkItem>>() as IWebHookSender);
            return harpoon;
        }

        public static IHarpoonBuilder ProcessNotificationsUsingLocalQueue(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.TryAddSingleton<IWebHookService, DefaultWebHookService>();
            harpoon.Services.TryAddSingleton<BackgroundQueue<IWebHookNotification>>();
            harpoon.Services.AddHostedService<QueuedHostedService<IWebHookNotification>>();
            return harpoon;
        }

        public static IHarpoonBuilder ProcessWebHookWorkItemsUsingLocalQueue(this IHarpoonBuilder harpoon)
        {
            harpoon.Services.TryAddSingleton<IWebHookSender, BackgroundSender>();
            harpoon.Services.TryAddSingleton<BackgroundQueue<IWebHookWorkItem>>();
            harpoon.Services.AddHostedService<QueuedHostedService<IWebHookWorkItem>>();
            return harpoon;
        }

        //Next : for rmq

        //public static IServiceCollection UseAllRmqDefaults(this IHarpoonBuilder harpoon) => harpoon.UseAllLocalDefaults(b => { });
        //public static IServiceCollection UseAllRmqDefaults(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> senderPolicy)
        //    => harpoon.SendNotificationsToRmq()
        //        .ReceiveNotificationsFromRmq()
        //        .UseDefaultNotificationProcessor()
        //        .SendWebHookWorkItemsToRmq()
        //        .ReceiveWebHookWorkItemsFromRmq()
        //        .UseDefaultWebHookWorkItemProcessor(senderPolicy)
        //        .ConfigureRmq()
        //        .Services;

        //public static IHarpoonBuilder SendNotificationsToRmq(this IHarpoonBuilder harpoon) => harpoon;
        //public static IHarpoonBuilder ReceiveNotificationsFromRmq(this IHarpoonBuilder harpoon) => harpoon;

        //public static IHarpoonBuilder SendWebHookWorkItemsToRmq(this IHarpoonBuilder harpoon) => harpoon;
        //public static IHarpoonBuilder ReceiveWebHookWorkItemsFromRmq(this IHarpoonBuilder harpoon) => harpoon;

        //public static IHarpoonBuilder ConfigureRmq(this IHarpoonBuilder harpoon) => harpoon;
    }
}