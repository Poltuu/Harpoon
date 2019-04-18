using System.Security.Principal;
using System.Threading.Tasks;

namespace Harpoon.Registration
{
    public interface IPrincipalIdGetter
    {
        Task<string> GetPrincipalIdForWebHookRegistrationAsync(IPrincipal principal);
    }
}
