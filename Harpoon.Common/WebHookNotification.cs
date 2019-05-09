using System.Collections.Generic;

namespace Harpoon
{
    public class WebHookNotification : IWebHookNotification
    {
        public string TriggerId { get; set; }

        public Dictionary<string, object> Payload { get; set; }
        IReadOnlyDictionary<string, object> IWebHookNotification.Payload => Payload;
    }
}
