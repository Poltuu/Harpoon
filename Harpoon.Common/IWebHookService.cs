using System.Threading;
using System.Threading.Tasks;

namespace Harpoon
{
    /// <summary>
    /// Represents a service able to handle an event and translate it appropriate webhooks.
    /// This is the entry point of a webhook pipeline.
    /// </summary>
    public interface IWebHookService
    {
        /// <summary>
        /// Start the notification process by adding the notification into a queue
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task NotifyAsync(IWebHookNotification notification, CancellationToken cancellationToken = default);
    }
}