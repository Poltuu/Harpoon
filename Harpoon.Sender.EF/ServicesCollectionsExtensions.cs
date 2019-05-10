using Harpoon;
using Harpoon.Background;
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
        public static IHarpoonBuilder UseDefaultEFWebHookWorkItemProcessor<TContext>(this IHarpoonBuilder harpoon)
            where TContext : DbContext, IRegistrationsContext 
            => harpoon.UseDefaultEFWebHookWorkItemProcessor<TContext>(b => { });

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