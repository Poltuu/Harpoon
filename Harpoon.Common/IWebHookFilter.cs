using System.Collections.Generic;

namespace Harpoon
{
    public interface IWebHookFilter
    {
        string ActionId { get; }
        IReadOnlyDictionary<string, object> Parameters { get; }
    }
}
