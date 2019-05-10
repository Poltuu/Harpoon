using System.Collections.Generic;
using System.Threading.Tasks;

namespace Harpoon
{
    public interface IWebHookStore
    {
        Task<IReadOnlyList<IWebHook>> GetApplicableWebHooksAsync(IWebHookNotification notification);
    }
}
