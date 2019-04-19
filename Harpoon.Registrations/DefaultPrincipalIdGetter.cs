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
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            string result = null;
            if (principal is ClaimsPrincipal claimsPrincipal)
            {
                result = claimsPrincipal?.FindFirst(ClaimTypes.Name)?.Value;
                if (result == null)
                {
                    result = claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }
            }

            if (result == null && principal.Identity != null)
            {
                result = principal.Identity.Name;
            }

            if (result == null)
            {
                throw new ArgumentException("Current principal id could not be found.");
            }

            return Task.FromResult(result);
        }
    }
}
