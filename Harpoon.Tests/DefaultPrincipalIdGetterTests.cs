using Harpoon.Registrations;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Harpoon.Tests
{
    public class DefaultPrincipalIdGetterTests
    {
        [Fact]
        public async Task ArgNull()
        {
            var service = new DefaultPrincipalIdGetter();

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetPrincipalIdForWebHookRegistrationAsync(null));
        }

        public static IEnumerable<object[]> NameScenario => new List<object[]>
        {
            new object[] { new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, "first"),
                new Claim(ClaimTypes.NameIdentifier, "second"),
                new Claim("NameType", "third"),
            }, "scheme", "NameType", ClaimTypes.Role)), "first" },
            new object[] { new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "second"),
                new Claim("NameType", "third"),
            }, "scheme", "NameType", ClaimTypes.Role)), "second" },
            new object[] { new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("NameType", "third"),
            }, "scheme", "NameType", ClaimTypes.Role)), "third" },
        };

        [Theory]
        [MemberData(nameof(NameScenario))]
        public async Task DefaultOrder(ClaimsPrincipal principal, string expectedName)
        {
            var service = new DefaultPrincipalIdGetter();
            Assert.Equal(expectedName, await service.GetPrincipalIdForWebHookRegistrationAsync(principal));

            principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { }, "scheme", "NameType", ClaimTypes.Role));
            await Assert.ThrowsAsync<ArgumentException>(() => service.GetPrincipalIdForWebHookRegistrationAsync(principal));
        }

        [Fact]
        public async Task NoNameThrows()
        {
            var service = new DefaultPrincipalIdGetter();

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { }, "scheme", "NameType", ClaimTypes.Role));
            await Assert.ThrowsAsync<ArgumentException>(() => service.GetPrincipalIdForWebHookRegistrationAsync(principal));
        }
    }
}