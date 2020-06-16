using Harpoon.Registrations;
using Harpoon.Registrations.EFStorage;
using Harpoon.Tests.Mocks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Tests.Fixtures
{
    public class DatabaseFixture : IDisposable
    {
        private IServiceProvider _provider;

        public IServiceProvider Provider => _provider ??= GetProvider();

        private IServiceProvider GetProvider()
        {
            var services = new ServiceCollection();
            services.AddEntityFrameworkSqlServer().AddDbContext<TestContext1>();

            var getter = new Mock<IPrincipalIdGetter>();
            getter.Setup(g => g.GetPrincipalIdAsync(It.IsAny<IPrincipal>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult("principal1"));
            services.AddSingleton(getter.Object);

            var protector = new Mock<IDataProtector>();
            protector.Setup(p => p.Protect(It.IsAny<byte[]>())).Returns<byte[]>(v => v);
            protector.Setup(p => p.Unprotect(It.IsAny<byte[]>())).Returns<byte[]>(v => v);
            protector.Setup(s => s.CreateProtector(It.IsAny<string>())).Returns(protector.Object);
            services.AddSingleton<IDataProtectionProvider>(protector.Object);
            services.AddSingleton<ISecretProtector, DefaultSecretProtector>();

            services.AddSingleton(new Mock<ILogger<WebHookRegistrationStore<TestContext1>>>().Object);
            services.AddSingleton<WebHookRegistrationStore<TestContext1>>();
            services.AddSingleton<WebHookStore<TestContext1>>();

            var result = services.BuildServiceProvider();

            result.GetRequiredService<TestContext1>().Database.EnsureCreated();

            return result;
        }

        public void Dispose()
        {
            if (_provider != null)
            {
                var context = _provider.GetRequiredService<TestContext1>();
                context.Database.EnsureDeleted();
            }
        }
    }
}