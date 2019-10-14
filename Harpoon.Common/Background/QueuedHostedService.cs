using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Background
{
    internal class QueuedHostedService<TWorkItem> : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<QueuedHostedService<TWorkItem>> _logger;

        private readonly BackgroundQueue<TWorkItem> _webHooksQueue;

        public QueuedHostedService(IServiceProvider services, ILogger<QueuedHostedService<TWorkItem>> logger, BackgroundQueue<TWorkItem> webHooksQueue)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webHooksQueue = webHooksQueue ?? throw new ArgumentNullException(nameof(webHooksQueue));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Queued Background Service of {typeof(TWorkItem).Name} is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _webHooksQueue.DequeueAsync(stoppingToken);

                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IQueuedProcessor<TWorkItem>>();
                        await service.ProcessAsync(workItem, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Queued Hosted Service of {typeof(TWorkItem).Name} error.");
                }
            }

            _logger.LogWarning($"Queued Hosted Service of {typeof(TWorkItem).Name} has been canceled by token.");
        }
    }
}