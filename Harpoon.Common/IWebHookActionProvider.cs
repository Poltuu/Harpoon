using System.Collections.Generic;
using System.Threading.Tasks;

namespace Harpoon
{
    public interface IWebHookActionProvider
    {
        Task<IReadOnlyDictionary<string, WebHookAction>> GetAvailableActionsAsync();
    }
}
