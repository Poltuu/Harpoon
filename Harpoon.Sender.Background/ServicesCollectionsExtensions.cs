using Harpoon;
using Harpoon.Sender;
using Harpoon.Sender.Background;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServicesCollectionsExtensions
    {
        public static IHarpoonBuilder UseDefaultSenderInBackground(this IHarpoonBuilder harpoon)
            => harpoon.UseDefaultSenderInBackground(b => { });

        public static IHarpoonBuilder UseDefaultSenderInBackground(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> webHookSender)
            => harpoon.UseSenderInBackground<DefaultWebHookSender>(webHookSender);

        public static IHarpoonBuilder UseSenderInBackground<TWebHookSender>(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> webHookSender)
            where TWebHookSender : class, IWebHookSender
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
            where TWebHookSender : class, IWebHookSender
        {
            harpoon.Services.TryAddSingleton<IWebHookSender, BackgroundSender>();
            harpoon.Services.AddSingleton<WebHooksQueue>();
            harpoon.Services.AddHostedService<QueuedHostedService<TWebHookSender>>();

            harpoon.Services.TryAddScoped<TWebHookSender>();

            return harpoon;
        }
    }
}