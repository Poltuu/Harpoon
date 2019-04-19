using Harpoon;
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
    }
}