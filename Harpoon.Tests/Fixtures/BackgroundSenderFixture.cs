using Harpoon.Sender.Background;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Tests.Fixtures
{
    public class BackgroundSenderFixture : IDisposable
    {
        public class FakeWebHookSender : IWebHookSender
        {
            public Task SendAsync(IWebHookNotification notification, IReadOnlyList<IWebHook> webHooks, CancellationToken token)
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
                 services.AddSingleton(new Mock<ILogger<QueuedHostedService<FakeWebHookSender>>>().Object);
                 services.AddScoped<FakeWebHookSender>();
                 services.AddHostedService<QueuedHostedService<FakeWebHookSender>>();
                 services.AddSingleton<WebHooksQueue>();
             }).Build();
            _host.Start();
        }

        public void Dispose()
        {
            Task.Run(() => _host.StopAsync());
        }
    }
}