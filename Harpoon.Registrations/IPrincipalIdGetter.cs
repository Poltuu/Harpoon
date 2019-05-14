using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Registrations
{
    /// <summary>
    /// Represents a class able to extract a string from a principal, that will be later used as an id for webhook registration
    /// </summary>
    public interface IPrincipalIdGetter
    {
        /// <summary>
        /// Extracts a string from given principal, later used as an id for webhook registration
        /// </summary>
        /// <param name="principal"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetPrincipalIdAsync(IPrincipal principal, CancellationToken cancellationToken = default);
    }
}
