using System.Threading.Tasks;

namespace Harpoon
{
    public interface IWebHookService
    {
        /// <summary>
        /// Start the notification process by adding the notification into a queue
        /// </summary>
        /// <param name="notification"></param>
        Task NotifyAsync(IWebHookNotification notification);
    }
}