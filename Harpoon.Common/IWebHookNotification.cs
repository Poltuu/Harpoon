using System.Collections.Generic;

namespace Harpoon
{
    public interface IWebHookNotification
    {
        string ActionId { get; }
        IReadOnlyDictionary<string, object> Payload { get; }
    }
}
