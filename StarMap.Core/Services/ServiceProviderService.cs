using Microsoft.Extensions.DependencyInjection;
using StarMap.Core.API;
using StarMap.Core.ModRepository;

namespace StarMap.Core.Services
{
    internal interface IServiceProviderService : IDisposable
    {
        IServiceProvider CreateModDependencies(RuntimeMod mod);
    }

    internal class ServiceProviderService(IServiceProvider _serviceProvider) : IServiceProviderService
    {
        private readonly Lock _lock = new();
        private readonly List<IServiceScope> _modScopes = [];

        public IServiceProvider CreateModDependencies(RuntimeMod mod)
        {
            var scope = _serviceProvider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IModContext>().Mod = mod;

            lock (_lock)
            {
                _modScopes.Add(scope);
            }

            return scope.ServiceProvider;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var scope in _modScopes)
                {
                    scope.Dispose();
                }
            }
        }
    }
}
