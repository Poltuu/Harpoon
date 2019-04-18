using Microsoft.Extensions.DependencyInjection;

namespace Harpoon
{
    public interface IHarpoonBuilder
    {
        IServiceCollection Services { get; }
    }
}