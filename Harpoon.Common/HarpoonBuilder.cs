using Microsoft.Extensions.DependencyInjection;

namespace Harpoon
{
    class HarpoonBuilder : IHarpoonBuilder
    {
        public IServiceCollection Services { get; set; }

        internal HarpoonBuilder() { }
    }
}