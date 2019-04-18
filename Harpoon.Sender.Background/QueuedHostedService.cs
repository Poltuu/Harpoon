using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Sender.Background
{
    public class QueuedHostedService<TWebHookSender> : BackgroundService
        where TWebHookSender : class, IWebHookSender
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<QueuedHostedService<TWebHookSender>> _logger;

        private readonly WebHooksQueue _webHooksQueue;

        public QueuedHostedService(IServiceProvider services, ILogger<QueuedHostedService<TWebHookSender>> logger, WebHooksQueue webHooksQueue)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webHooksQueue = webHooksQueue ?? throw new ArgumentNullException(nameof(webHooksQueue));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var (notification, webHooks) = await _webHooksQueue.DequeueAsync(stoppingToken);

                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<TWebHookSender>();
                        await service.SendAsync(notification, webHooks);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Queued Hosted Service error.");
                }
            }

            _logger.LogInformation("Queued Hosted Service is stopping.");
        }
    }
}