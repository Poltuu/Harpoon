using Microsoft.Extensions.DependencyInjection;

namespace Harpoon
{
    public class HarpoonBuilder : IHarpoonBuilder
    {
        public IServiceCollection Services { get; set; }
        internal HarpoonBuilder() { }
    }
}