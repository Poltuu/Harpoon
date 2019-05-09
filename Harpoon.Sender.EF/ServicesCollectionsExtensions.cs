using Harpoon;
using Harpoon.Registrations.EFStorage;
using Harpoon.Sender;
using Harpoon.Sender.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServicesCollectionsExtensions
    {
        public static IHarpoonBuilder UseDefaultEFSender<TContext>(this IHarpoonBuilder harpoon)
            where TContext : DbContext, IRegistrationsContext
            => harpoon.UseDefaultEFSender<TContext>(b => { });

        public static IHarpoonBuilder UseDefaultEFSender<TContext>(this IHarpoonBuilder harpoon, Action<IHttpClientBuilder> webHookSender)
            where TContext : DbContext, IRegistrationsContext
        {
            if (webHookSender == null)
            {
                throw new ArgumentNullException(nameof(webHookSender));
            }

            harpoon.Services.TryAddScoped<IWebHookSender, EFWebHookSender<TContext>>();
            harpoon.Services.TryAddSingleton<ISignatureService, DefaultSignatureService>();

            var httpClientBuilder = harpoon.Services.AddHttpClient<IWebHookSender, EFWebHookSender<TContext>>();
            webHookSender(httpClientBuilder);

            return harpoon;
        }
    }
}