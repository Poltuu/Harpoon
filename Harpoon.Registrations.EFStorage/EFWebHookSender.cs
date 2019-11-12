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
    /// <see cref="IWebHookSender"/> implementation that automatically pauses webhooks on NotFound responses
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
        protected override Task OnFailureAsync(HttpResponseMessage response, Exception exception, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
        {
            return AddLogAsync(webHookWorkItem, $"WebHook {webHookWorkItem.WebHook.Id} failed. [{webHookWorkItem.WebHook.Callback}]: {exception.Message}");
        }

        /// <inheritdoc />
        protected override Task OnSuccessAsync(HttpResponseMessage response, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
        {
            return AddLogAsync(webHookWorkItem);
        }

        /// <inheritdoc />
        protected override async Task OnNotFoundAsync(HttpResponseMessage response, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
        {
            var dbWebHook = await _context.WebHooks.FirstOrDefaultAsync(w => w.Id == webHookWorkItem.WebHook.Id);
            if (dbWebHook != null)
            {
                dbWebHook.IsPaused = true;
            }

            await AddLogAsync(webHookWorkItem, $"WebHook {webHookWorkItem.WebHook.Id} was paused. [{webHookWorkItem.WebHook.Callback}]");
        }

        private async Task AddLogAsync(IWebHookWorkItem workItem, string error = null)
        {
            var log = new WebHookLog
            {
                Error = error,
                WebHookId = workItem.WebHook.Id,
                WebHookNotificationId = workItem.Id
            };
            _context.Add(log);

            try
            {
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(error))
                {
                    Logger.LogInformation(error);
                }
            }
            catch (Exception e)
            {
                if (!string.IsNullOrEmpty(error))
                {
                    Logger.LogError(error);
                }

                Logger.LogError($"Log failed for WebHook {workItem.WebHook.Id}. [{workItem.WebHook.Callback}]: {e.Message}");
            }
        }
    }
}