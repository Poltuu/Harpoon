using System;
using System.Net.Http;
using System.Threading.Tasks;
using Harpoon.Registrations.EFStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Harpoon.Sender.EF
{
    public class EFWebHookSender<TContext> : DefaultWebHookSender
        where TContext : DbContext, IRegistrationsContext
    {
        private readonly TContext _context;

        public EFWebHookSender(HttpClient httpClient, ISignatureService signatureService, ILogger<DefaultWebHookSender> logger, TContext context)
            : base(httpClient, signatureService, logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        protected override async Task OnNotFoundAsync(IWebHookNotification notification, IWebHook webHook)
        {
            var dbWebHook = await _context.WebHooks.FirstOrDefaultAsync(w => w.Id == webHook.Id);
            if (dbWebHook == null)
            {
                return;
            }

            dbWebHook.IsPaused = true;
            await _context.SaveChangesAsync();

            Logger.LogInformation($"WebHook {webHook.Id} was paused.");
        }
    }
}