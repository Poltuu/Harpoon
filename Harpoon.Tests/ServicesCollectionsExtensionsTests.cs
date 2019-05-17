using Harpoon.Background;
using Harpoon.Controllers.Swashbuckle;
using Harpoon.Registrations;
using Harpoon.Sender;
using Harpoon.Tests.Mocks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using Xunit;

namespace Harpoon.Tests
{
    public class ServicesCollectionsExtensionsTests
    {
        class TestWebHookTriggerProvider : IWebHookTriggerProvider
        {
            public IReadOnlyDictionary<string, WebHookTrigger> GetAvailableTriggers()
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void NullArgTests()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon(null));
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon(b => b.UseDefaultWebHookWorkItemProcessor(null)));
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon(b => b.UseDefaultValidator(null)));
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon(b => b.UseDefaultDataProtection(null, c => { })));
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon(b => b.UseDefaultDataProtection(c => { }, null)));
            Assert.Throws<ArgumentNullException>(() => services.AddHarpoon(b => b.UseDefaultEFWebHookWorkItemProcessor<TestContext1>(null)));

            Assert.Throws<ArgumentNullException>(() => ModelBuilderExtensions.AddWebHookDefaultMapping(null));
            Assert.Throws<ArgumentNullException>(() => ModelBuilderExtensions.AddWebHookFilterDefaultMapping(null));
        }

        [Fact]
        public void AddHarpoonUseAllSynchronousDefaultsTests()
        {
            var services = new ServiceCollection();
            services.AddHarpoon(b => b.UseAllSynchronousDefaults());
            services.AddSingleton(new Mock<IWebHookStore>().Object);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IWebHookService>());
            Assert.NotNull(provider.GetRequiredService<IQueuedProcessor<IWebHookNotification>>());
            Assert.NotNull(provider.GetRequiredService<IWebHookSender>());
            Assert.NotNull(provider.GetRequiredService<IQueuedProcessor<IWebHookWorkItem>>());
            Assert.NotNull(provider.GetRequiredService<ISignatureService>());
        }

        [Fact]
        public void AddHarpoonUseAllLocalDefaultsTests()
        {
            var services = new ServiceCollection();
            services.AddHarpoon(b => b.UseAllLocalDefaults());
            services.AddSingleton(new Mock<IWebHookStore>().Object);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IWebHookService>());
            Assert.NotNull(provider.GetRequiredService<BackgroundQueue<IWebHookNotification>>());
            Assert.NotNull(provider.GetRequiredService<IEnumerable<IHostedService>>());
            Assert.NotNull(provider.GetRequiredService<IQueuedProcessor<IWebHookNotification>>());
            Assert.NotNull(provider.GetRequiredService<IWebHookSender>());
            Assert.NotNull(provider.GetRequiredService<BackgroundQueue<IWebHookWorkItem>>());
            Assert.NotNull(provider.GetRequiredService<IQueuedProcessor<IWebHookWorkItem>>());
            Assert.NotNull(provider.GetRequiredService<ISignatureService>());
        }

        [Fact]
        public void AddHarpoonRegisterWebHooksUsingEfStorageTests()
        {
            var services = new ServiceCollection();
            services.AddEntityFrameworkSqlServer().AddDbContext<TestContext1>();
            services.AddHarpoon(h =>
            {
                h.RegisterWebHooksUsingEfStorage<TestContext1, TestWebHookTriggerProvider>();
                h.UseDefaultDataProtection(b => b.UseEphemeralDataProtectionProvider(), o => { });
                h.UseDefaultValidator();
            });

            services.AddSingleton(new Mock<IWebHookTriggerProvider>().Object);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IWebHookValidator>());
            Assert.NotNull(provider.GetRequiredService<IWebHookTriggerProvider>());
            Assert.NotNull(provider.GetRequiredService<IPrincipalIdGetter>());
            Assert.NotNull(provider.GetRequiredService<IWebHookStore>());
            Assert.NotNull(provider.GetRequiredService<IWebHookRegistrationStore>());
            Assert.NotNull(provider.GetRequiredService<IWebHookMatcher>());
        }

        [Fact]
        public void AddHarpoonWithEfSenderTests()
        {
            var services = new ServiceCollection();
            services.AddEntityFrameworkSqlServer().AddDbContext<TestContext1>();
            services.AddHarpoon(h => h.UseDefaultEFWebHookWorkItemProcessor<TestContext1>());

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<IQueuedProcessor<IWebHookWorkItem>>());
            Assert.NotNull(provider.GetRequiredService<ISignatureService>());
        }

        [Fact]
        public void AddHarpoonDocumentation()
        {
            var options = new SwaggerGenOptions();
            options.AddHarpoonDocumentation();

            //do not know what to test here.
        }
    }
}