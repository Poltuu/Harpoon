using System.Collections.Generic;

namespace Harpoon
{
    /// <inheritdoc />
    public class WebHookNotification : IWebHookNotification
    {
        /// <inheritdoc />
        public string TriggerId { get; set; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Payload { get; set; }
    }
}
