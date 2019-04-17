using System.Threading.Tasks;

namespace Harpoon
{
    public interface IWebHookService
    {
        Task<int> NotifyAsync(IWebHookNotification notification);
    }
}