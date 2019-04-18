using System.Collections.Generic;

namespace Harpoon
{
    public class WebHookNotification : IWebHookNotification
    {
        public string ActionId { get; set; }

        public Dictionary<string, object> Payload { get; set; }
        IReadOnlyDictionary<string, object> IWebHookNotification.Payload => Payload;
    }
}
