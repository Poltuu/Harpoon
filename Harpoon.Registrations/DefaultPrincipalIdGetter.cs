using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Harpoon.Registrations
{
    public class DefaultPrincipalIdGetter : IPrincipalIdGetter
    {
        public Task<string> GetPrincipalIdForWebHookRegistrationAsync(IPrincipal principal)
        {
            return Task.FromResult(GetPrincipalIdForWebHookRegistration(principal));
        }

        private string GetPrincipalIdForWebHookRegistration(IPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            if (principal is ClaimsPrincipal claimsPrincipal)
            {
                if (TryGetNotNullClaimValue(claimsPrincipal, ClaimTypes.Name, out var name))
                {
                    return name;
                }

                if (TryGetNotNullClaimValue(claimsPrincipal, ClaimTypes.NameIdentifier, out var nameIdentifier))
                {
                    return nameIdentifier;
                }
            }

            if (principal.Identity?.Name != null)
            {
                return principal.Identity.Name;
            }

            throw new ArgumentException("Current principal id could not be found.");
        }

        private bool TryGetNotNullClaimValue(ClaimsPrincipal principal, string claimType, out string result)
        {
            result = principal.FindFirst(claimType)?.Value;
            return result != null;
        }
    }
}
