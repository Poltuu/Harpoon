using Microsoft.Extensions.DependencyInjection;

namespace Harpoon
{
    /// <summary>
    /// Harpoon configuration builder. Extensions methods should come from the same namespace.
    /// </summary>
    public interface IHarpoonBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> used for registration
        /// </summary>
        IServiceCollection Services { get; }
    }
}