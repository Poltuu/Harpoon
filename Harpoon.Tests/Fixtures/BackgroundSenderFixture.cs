using Harpoon.Background;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Tests.Fixtures
{
    public class BackgroundSenderFixture : IDisposable
    {
        public class FakeWebHookSender : IQueuedProcessor<IWebHookWorkItem>
        {
            public Task ProcessAsync(IWebHookWorkItem workItem, CancellationToken token)
            {
                FakeWebHookSenderCount++;
                return Task.CompletedTask;
            }
        }

        public static int FakeWebHookSenderCount { get; private set; }

        private readonly IHost _host;
        public IServiceProvider Services => _host.Services;

        public BackgroundSenderFixture()
        {
            _host = new HostBuilder().ConfigureServices(services =>
             {
                 services.AddSingleton(new Mock<ILogger<QueuedHostedService<IWebHookWorkItem>>>().Object);
                 services.AddScoped<IQueuedProcessor<IWebHookWorkItem>, FakeWebHookSender>();
                 services.AddHostedService<QueuedHostedService<IWebHookWorkItem>>();
                 services.AddSingleton<BackgroundQueue<IWebHookWorkItem>>();
             }).Build();
            _host.Start();
        }

        public void Dispose()
        {
            Task.Run(() => _host.StopAsync());
        }
    }
}