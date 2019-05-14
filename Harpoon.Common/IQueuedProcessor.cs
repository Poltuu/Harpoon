using System.Threading;
using System.Threading.Tasks;

namespace Harpoon
{
    /// <summary>
    /// Represents a class able to consume workItems
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IQueuedProcessor<T>
    {
        /// <summary>
        /// Process workItem into the webhook pipeline
        /// </summary>
        /// <param name="workItem"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ProcessAsync(T workItem, CancellationToken cancellationToken);
    }
}