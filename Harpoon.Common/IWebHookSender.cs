using System.Collections.Generic;
using System.Threading.Tasks;

namespace Harpoon
{
    public interface IWebHookSender
    {
        Task SendAsync(IWebHookNotification notification, IReadOnlyList<IWebHook> webHooks);
    }
}