using System.Collections.Generic;

namespace Harpoon
{
    public interface IWebHookNotification
    {
        string TriggerId { get; }
        IReadOnlyDictionary<string, object> Payload { get; }
    }
}
