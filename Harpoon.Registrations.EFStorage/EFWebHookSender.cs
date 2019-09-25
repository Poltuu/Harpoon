using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Harpoon.Registrations.EFStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Harpoon.Sender.EF
{
    /// <summary>
    /// <see cref="IWebHookSender"/> that automatically pauses webhooks on NotFound responses
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public class EFWebHookSender<TContext> : DefaultWebHookSender
        where TContext : DbContext, IRegistrationsContext
    {
        private readonly TContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFWebHookSender{TContext}"/> class.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="signatureService"></param>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        public EFWebHookSender(HttpClient httpClient, ISignatureService signatureService, ILogger<DefaultWebHookSender> logger, TContext context)
            : base(httpClient, signatureService, logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        protected override async Task OnNotFoundAsync(HttpResponseMessage response, IWebHookWorkItem workItem, CancellationToken cancellationToken)
        {
            var dbWebHook = await _context.WebHooks.FirstOrDefaultAsync(w => w.Id == workItem.WebHook.Id, cancellationToken);
            if (dbWebHook == null)
            {
                return;
            }

            dbWebHook.IsPaused = true;
            await _context.SaveChangesAsync(cancellationToken);

            Logger.LogInformation($"WebHook {workItem.WebHook.Id} was paused. [{workItem.WebHook.Callback}]");
        }
    }
}