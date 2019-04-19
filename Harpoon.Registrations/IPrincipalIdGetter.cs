using System.Security.Principal;
using System.Threading.Tasks;

namespace Harpoon.Registrations
{
    public interface IPrincipalIdGetter
    {
        Task<string> GetPrincipalIdForWebHookRegistrationAsync(IPrincipal principal);
    }
}
