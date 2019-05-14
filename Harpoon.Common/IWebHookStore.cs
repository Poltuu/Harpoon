using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon
{
    /// <summary>
    /// Represents a class able to return the <see cref="IWebHook"/> matching a specific <see cref="IWebHookNotification"/>
    /// </summary>
    public interface IWebHookStore
    {
        /// <summary>
        /// Returns the <see cref="IWebHook"/> matching a specific <see cref="IWebHookNotification"/>
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IReadOnlyList<IWebHook>> GetApplicableWebHooksAsync(IWebHookNotification notification, CancellationToken cancellationToken = default);
    }
}
