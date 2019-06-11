using System.Collections.Generic;

namespace Harpoon
{
    /// <inheritdoc />
    public class WebHookNotification : IWebHookNotification
    {
        /// <inheritdoc />
        public string TriggerId { get; set; }

        /// <inheritdoc />
        public Dictionary<string, object> Payload { get; set; }

        IReadOnlyDictionary<string, object> IWebHookNotification.Payload => Payload;
    }
}
