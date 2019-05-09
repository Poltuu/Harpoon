using System.Collections.Generic;

namespace Harpoon
{
    public interface IWebHookFilter
    {
        string TriggerId { get; }
        IReadOnlyDictionary<string, object> Parameters { get; }
    }
}
