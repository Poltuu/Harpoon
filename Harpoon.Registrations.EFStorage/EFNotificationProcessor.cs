using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Harpoon.Registrations.EFStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Harpoon.Sender.EF
{
    /// <summary>
    /// <see cref="IQueuedProcessor{IWebHookNotification}"/> implementation that logs everything into the context
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public class EFNotificationProcessor<TContext> : DefaultNotificationProcessor
        where TContext : DbContext, IRegistrationsContext
    {
        private readonly TContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFNotificationProcessor{TContext}"/> class.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="webHookStore"></param>
        /// <param name="webHookSender"></param>
        /// <param name="logger"></param>
        public EFNotificationProcessor(TContext context, IWebHookStore webHookStore, IWebHookSender webHookSender, ILogger<DefaultNotificationProcessor> logger)
            : base(webHookStore, webHookSender, logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        protected override async Task<Guid> LogAsync(IWebHookNotification notification, IReadOnlyList<IWebHook> webHooks, CancellationToken cancellationToken)
        {
            var notif = new Registrations.EFStorage.WebHookNotification
            {
                Payload = notification.Payload,
                TriggerId = notification.TriggerId,
                Count = webHooks.Count
            };
            _context.Add(notif);

            await  _context.SaveChangesAsync();
            return notif.Id;
        }
    }
}