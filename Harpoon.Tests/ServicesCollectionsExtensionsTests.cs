using Harpoon.Registrations;
using Harpoon.Sender;
using Harpoon.Sender.Background;
using Harpoon.Tests.Mocks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class ServicesCollectionsExtensionsTests
    {
        class TestWebHookTriggerProvider : IWebHookTriggerProvider
        {
            public Task<IReadOnlyDictionary<string, WebHookTrigger>> GetAvailableTriggersAsync()
            {
                throw new System.NotImplementedException();
            }
        }

        [Fact]
        public void NullArgTests()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon(null));
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon().UseDefaultSender(null));
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon().UseDefaultSenderInBackground(null));
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon().UseEfStorage<TestContext1>(null));
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon().UseEfStorage<TestContext1>((p, b) => { }, null));
        }

        [Fact]
        public void AddHarpoonTests()
        {
            var services = new ServiceCollection();
            services.AddHarpoon();

            services.AddSingleton(new Mock<IWebHookStore>().Object);
            services.AddSingleton(new Mock<IWebHookSender>().Object); 
            services.AddSingleton(new Mock<IWebHookTriggerProvider>().Object);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IWebHookService>());
            Assert.NotNull(provider.GetRequiredService<IWebHookValidator>());
        }

        [Fact]
        public void AddHarpoonWithEfTests()
        {
            var services = new ServiceCollection();
            services.AddEntityFrameworkSqlServer().AddDbContext<TestContext1>();
            services.AddHarpoon().UseEfStorage<TestContext1, TestWebHookTriggerProvider>((purpose, b) => b.UseEphemeralDataProtectionProvider());

            services.AddSingleton(new Mock<IWebHookSender>().Object);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IWebHookService>());
            Assert.NotNull(provider.GetRequiredService<IWebHookValidator>());
            Assert.NotNull(provider.GetRequiredService<IWebHookTriggerProvider>());
            Assert.NotNull(provider.GetRequiredService<IPrincipalIdGetter>());
            Assert.NotNull(provider.GetRequiredService<IWebHookStore>());
            Assert.NotNull(provider.GetRequiredService<IWebHookRegistrationStore>());
        }

        [Fact]
        public void AddHarpoonWithEfSenderTests()
        {
            var services = new ServiceCollection();
            services.AddEntityFrameworkSqlServer().AddDbContext<TestContext1>();
            services.AddHarpoon().UseDefaultEFSender<TestContext1>();

            services.AddSingleton(new Mock<IWebHookStore>().Object);
            services.AddSingleton(new Mock<IWebHookTriggerProvider>().Object);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IWebHookService>());
            Assert.NotNull(provider.GetRequiredService<ISignatureService>());
            Assert.NotNull(provider.GetRequiredService<IWebHookValidator>());
            Assert.NotNull(provider.GetRequiredService<IWebHookSender>());
        }

        [Fact]
        public void AddHarpoonWithDefaultSender()
        {
            var services = new ServiceCollection();
            services.AddHarpoon().UseDefaultSender();

            services.AddSingleton(new Mock<IWebHookStore>().Object);
            services.AddSingleton(new Mock<IWebHookTriggerProvider>().Object);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IWebHookService>());
            Assert.NotNull(provider.GetRequiredService<ISignatureService>());
            Assert.NotNull(provider.GetRequiredService<IWebHookValidator>());
            Assert.NotNull(provider.GetRequiredService<IWebHookSender>());
        }

        [Fact]
        public void AddHarpoonWithBackgroundSender()
        {
            var services = new ServiceCollection();
            services.AddHarpoon().UseDefaultSenderInBackground();

            services.AddSingleton(new Mock<IWebHookStore>().Object);
            services.AddSingleton(new Mock<IWebHookTriggerProvider>().Object);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IWebHookService>());
            Assert.NotNull(provider.GetRequiredService<IWebHookValidator>());
            Assert.NotNull(provider.GetRequiredService<IWebHookSender>());
            Assert.NotNull(provider.GetRequiredService<WebHooksQueue>());
            Assert.NotNull(provider.GetRequiredService<IHostedService>());
        }
    }
}