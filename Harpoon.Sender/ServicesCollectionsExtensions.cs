using Harpoon;
using Harpoon.Sender;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServicesCollectionsExtensions
    {
        public static IHarpoonBuilder UseDefaultSender(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> webHookSender)
        {
            harpoon.Services.TryAddScoped<IWebHookSender, DefaultWebHookSender>();

            var httpClientBuilder = harpoon.Services.AddHttpClient<DefaultWebHookSender>();
            webHookSender?.Invoke(httpClientBuilder);

            return harpoon;
        }
    }
}