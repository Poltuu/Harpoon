using Harpoon.Registrations;
using Harpoon.Registrations.EFStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Harpoon.Tests
{
    public class DatabaseFixture : IDisposable
    {
        private IServiceProvider _provider;

        public IServiceProvider Provider => _provider ?? (_provider = GetProvider());

        private IServiceProvider GetProvider()
        {
            var services = new ServiceCollection();
            services.AddEntityFrameworkSqlServer().AddDbContext<TestContext>();

            var getter = new Mock<IPrincipalIdGetter>();
            getter.Setup(g => g.GetPrincipalIdAsync(It.IsAny<IPrincipal>())).Returns(Task.FromResult("principal1"));
            services.AddSingleton(getter.Object);

            var protector = new Mock<IDataProtector>();
            protector.Setup(p => p.Protect(It.IsAny<byte[]>())).Returns<byte[]>(v => v);
            protector.Setup(p => p.Unprotect(It.IsAny<byte[]>())).Returns<byte[]>(v => v);
            protector.Setup(s => s.CreateProtector(It.IsAny<string>())).Returns(protector.Object);
            services.AddSingleton<IDataProtectionProvider>(protector.Object);

            services.AddSingleton(new Mock<ILogger<WebHookRegistrationStore<TestContext>>>().Object);
            services.AddSingleton<WebHookRegistrationStore<TestContext>>();

            var result = services.BuildServiceProvider();

            var context = result.GetRequiredService<TestContext>();
            context.Database.EnsureCreated();

            return result;
        }

        public void Dispose()
        {
            if (_provider != null)
            {
                var context = _provider.GetRequiredService<TestContext>();
                context.Database.EnsureDeleted();
            }
        }
    }
}