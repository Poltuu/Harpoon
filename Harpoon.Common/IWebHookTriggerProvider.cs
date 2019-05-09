using System.Collections.Generic;
using System.Threading.Tasks;

namespace Harpoon
{
    public interface IWebHookTriggerProvider
    {
        Task<IReadOnlyDictionary<string, WebHookTrigger>> GetAvailableTriggersAsync();
    }
}
