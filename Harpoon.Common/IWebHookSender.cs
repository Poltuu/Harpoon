using System.Threading;
using System.Threading.Tasks;

namespace Harpoon
{
    /// <summary>
    /// Represents a class able to generates http calls from a <see cref="IWebHookWorkItem"/>
    /// </summary>
    public interface IWebHookSender
    {
        /// <summary>
        /// Generates an http call matching a <see cref="IWebHookWorkItem"/>
        /// </summary>
        /// <param name="webHookWorkItem"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendAsync(IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken = default);
    }
}