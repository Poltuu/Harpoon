using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Harpoon
{
    /// <summary>
    /// Represents a class able to returns valid <see cref="WebHookTrigger"/> available for registration.
    /// </summary>
    public interface IWebHookTriggerProvider
    {
        /// <summary>
        /// Returns valid <see cref="WebHookTrigger"/> available for registration.
        /// This is used to validate <see cref="IWebHook"/> registration.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IReadOnlyDictionary<string, WebHookTrigger>> GetAvailableTriggersAsync(CancellationToken cancellationToken = default);
    }
}
