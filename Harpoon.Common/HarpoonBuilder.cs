using Microsoft.Extensions.DependencyInjection;

namespace Harpoon
{
    class HarpoonBuilder : IHarpoonBuilder
    {
        public IServiceCollection Services { get; }

        internal HarpoonBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}