using System.Threading;
using System.Threading.Tasks;

namespace Harpoon
{
    public interface IWebHookSender
    {
        Task SendAsync(IWebHookWorkItem webHookWorkItem, CancellationToken token);
    }
}