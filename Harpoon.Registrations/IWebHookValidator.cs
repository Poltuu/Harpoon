using System.Threading;
using System.Threading.Tasks;

namespace Harpoon.Registrations
{
    /// <summary>
    /// Represents a class able to throws an exception if an <see cref="IWebHook"/> is invalid.
    /// </summary>
    public interface IWebHookValidator
    {
        /// <summary>
        /// Throws an exception if the given <see cref="IWebHook"/> is invalid.
        /// </summary>
        /// <param name="webHook"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ValidateAsync(IWebHook webHook, CancellationToken cancellationToken = default);
    }
}