using Harpoon.Registrations;
using Harpoon.Registrations.EFStorage;
using Harpoon.Sender.Background;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class ServicesCollectionsExtensionsTests
    {
        class TestContext : DbContext, IRegistrationsContext
        {
            public DbSet<Registration> Registrations { get; set; }
            IQueryable<Registration> IRegistrationsContext.Registrations => Registrations;
        }

        class TestWebHookActionProvider : IWebHookActionProvider
        {
            public Task<IReadOnlyDictionary<string, WebHookAction>> GetAvailableActionsAsync()
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
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon().UseEfStorage<TestContext>(null));
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon().UseEfStorage<TestContext>((p, b) => { }, null));
        }

        [Fact]
        public void AddHarpoonTests()
        {
            var services = new ServiceCollection();
            services.AddHarpoon();

            services.AddSingleton(new Mock<IWebHookStore>().Object);
            services.AddSingleton(new Mock<IWebHookSender>().Object); 
            services.AddSingleton(new Mock<IWebHookActionProvider>().Object);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IWebHookService>());
            Assert.NotNull(provider.GetRequiredService<IWebHookValidator>());
        }

        [Fact]
        public void AddHarpoonWithEfTests()
        {
            var services = new ServiceCollection();
            services.AddEntityFrameworkInMemoryDatabase().AddDbContext<TestContext>();
            services.AddHarpoon().UseEfStorage<TestContext, TestWebHookActionProvider>((purpose, b) => b.UseEphemeralDataProtectionProvider());

            services.AddSingleton(new Mock<IWebHookSender>().Object);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IWebHookService>());
            Assert.NotNull(provider.GetRequiredService<IWebHookValidator>());
            Assert.NotNull(provider.GetRequiredService<IWebHookActionProvider>());
            Assert.NotNull(provider.GetRequiredService<IPrincipalIdGetter>());
            Assert.NotNull(provider.GetRequiredService<IWebHookStore>());
            Assert.NotNull(provider.GetRequiredService<IWebHookRegistrationStore>());
        }

        [Fact]
        public void AddHarpoonWithDefaultSender()
        {
            var services = new ServiceCollection();
            services.AddHarpoon().UseDefaultSender();

            services.AddSingleton(new Mock<IWebHookStore>().Object);
            services.AddSingleton(new Mock<IWebHookActionProvider>().Object);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IWebHookService>());
            Assert.NotNull(provider.GetRequiredService<IWebHookValidator>());
            Assert.NotNull(provider.GetRequiredService<IWebHookSender>());
        }

        [Fact]
        public void AddHarpoonWithBackgroundSender()
        {
            var services = new ServiceCollection();
            services.AddHarpoon().UseDefaultSenderInBackground();

            services.AddSingleton(new Mock<IWebHookStore>().Object);
            services.AddSingleton(new Mock<IWebHookActionProvider>().Object);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IWebHookService>());
            Assert.NotNull(provider.GetRequiredService<IWebHookValidator>());
            Assert.NotNull(provider.GetRequiredService<IWebHookSender>());
            Assert.NotNull(provider.GetRequiredService<WebHooksQueue>());
            Assert.NotNull(provider.GetRequiredService<IHostedService>());
        }
    }
}