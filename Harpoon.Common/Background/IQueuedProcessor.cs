using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Background
{
    public interface IQueuedProcessor<T>
    {
        Task ProcessAsync(T workItem, CancellationToken token);
    }
}