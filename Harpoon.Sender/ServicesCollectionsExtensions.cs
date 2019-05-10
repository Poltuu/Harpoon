using Harpoon;
using Harpoon.Background;
using Harpoon.Sender;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServicesCollectionsExtensions
    {
        public static IHarpoonBuilder UseDefaultSender(this IHarpoonBuilder harpoon)
            => harpoon.UseDefaultSender(b => { });

        public static IHarpoonBuilder UseDefaultSender(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> webHookSender)
        {
            if (webHookSender == null)
            {
                throw new ArgumentNullException(nameof(webHookSender));
            }

            harpoon.Services.TryAddScoped<IWebHookSender, DefaultWebHookSender>();
            harpoon.Services.TryAddSingleton<ISignatureService, DefaultSignatureService>();

            var httpClientBuilder = harpoon.Services.AddHttpClient<IWebHookSender, DefaultWebHookSender>();
            webHookSender(httpClientBuilder);

            return harpoon;
        }

        public static IHarpoonBuilder UseDefaultSenderInBackground(this IHarpoonBuilder harpoon)
            => harpoon.UseDefaultSenderInBackground(b => { });

        public static IHarpoonBuilder UseDefaultSenderInBackground(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> webHookSender)
            => harpoon.UseSenderInBackground<DefaultWebHookSender>(webHookSender);

        public static IHarpoonBuilder UseSenderInBackground<TWebHookSender>(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> webHookSender)
            where TWebHookSender : class, IQueuedProcessor<IWebHookWorkItem>
        {
            if (webHookSender == null)
            {
                throw new ArgumentNullException(nameof(webHookSender));
            }

            harpoon.UseSenderInBackground<TWebHookSender>();

            webHookSender(harpoon.Services.AddHttpClient<TWebHookSender>());

            return harpoon;
        }

        public static IHarpoonBuilder UseSenderInBackground<TWebHookSender>(this IHarpoonBuilder harpoon)
            where TWebHookSender : class, IQueuedProcessor<IWebHookWorkItem>
        {
            harpoon.Services.TryAddSingleton<IWebHookSender, BackgroundSender>();
            harpoon.Services.AddSingleton<BackgroundQueue<(IWebHookNotification, IWebHook)>>();
            harpoon.Services.AddHostedService<QueuedHostedService<(IWebHookNotification, IWebHook)>>();

            harpoon.Services.TryAddScoped<TWebHookSender>();

            return harpoon;
        }
    }
}