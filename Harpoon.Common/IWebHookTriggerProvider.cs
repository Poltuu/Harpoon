using System.Collections.Generic;

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
        /// Typical implementation is expected to be in-memory
        /// </summary>
        /// <returns></returns>
        IReadOnlyDictionary<string, WebHookTrigger> GetAvailableTriggers();
    }
}
