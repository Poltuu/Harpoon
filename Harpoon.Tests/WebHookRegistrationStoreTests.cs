using Harpoon.Registrations;
using Harpoon.Registrations.EFStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class WebHookRegistrationStoreTests
    {
        [Fact]
        public void ArgNull()
        {
            var getter = new Mock<IPrincipalIdGetter>();
            var dataprotection = new Mock<IDataProtectionProvider>();
            dataprotection.Setup(s => s.CreateProtector(It.IsAny<string>())).Returns(new Mock<IDataProtector>().Object);
            var logger = new Mock<ILogger<WebHookRegistrationStore<TestContext>>>();

            Assert.Throws<ArgumentNullException>(() => new WebHookRegistrationStore<TestContext>(null, getter.Object, dataprotection.Object, logger.Object));
            Assert.Throws<ArgumentNullException>(() => new WebHookRegistrationStore<TestContext>(new TestContext(), null, dataprotection.Object, logger.Object));
            Assert.Throws<ArgumentNullException>(() => new WebHookRegistrationStore<TestContext>(new TestContext(), getter.Object, null, logger.Object));
            Assert.Throws<ArgumentNullException>(() => new WebHookRegistrationStore<TestContext>(new TestContext(), getter.Object, dataprotection.Object, null));

            dataprotection.Setup(s => s.CreateProtector(It.IsAny<string>())).Returns((IDataProtector)null);
            Assert.Throws<ArgumentNullException>(() => new WebHookRegistrationStore<TestContext>(new TestContext(), getter.Object, dataprotection.Object, logger.Object));

        }

        private IServiceProvider GetProvider()
        {
            var services = new ServiceCollection();
            services.AddEntityFrameworkInMemoryDatabase().AddDbContext<TestContext>();

            var getter = new Mock<IPrincipalIdGetter>();
            getter.Setup(g => g.GetPrincipalIdForWebHookRegistrationAsync(It.IsAny<IPrincipal>())).Returns(Task.FromResult("name"));
            services.AddSingleton(getter.Object);

            var protector = new Mock<IDataProtector>();
            protector.Setup(p => p.Protect(It.IsAny<string>())).Returns<string>(v => v);
            protector.Setup(p => p.Unprotect(It.IsAny<string>())).Returns<string>(v => v);
            protector.Setup(s => s.CreateProtector(It.IsAny<string>())).Returns(protector.Object);
            services.AddSingleton<IDataProtectionProvider>(protector.Object);

            services.AddSingleton(new Mock<ILogger<WebHookRegistrationStore<TestContext>>>().Object);
            services.AddSingleton<WebHookRegistrationStore<TestContext>>();

            return services.BuildServiceProvider();
        }

        private void Seed(TestContext context)
        {

        }

        [Fact]
        public void GetAllTests()
        {
            var services = GetProvider();
            var context = services.GetRequiredService<TestContext>();
            Seed(context);
            var store = services.GetRequiredService<WebHookRegistrationStore<TestContext>>();
            //TODO

        }
    }
}