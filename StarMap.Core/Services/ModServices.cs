using StarMap.Core.ModRepository;
using System.Collections.Concurrent;
using System.Runtime.Loader;

namespace StarMap.Core.Services
{
    internal interface IModServices
    {
        void Register<T>(RuntimeMod mod, T implementation);
        T Get<T>();
    }

    internal class ModServices : IModServices
    {
        private readonly ConcurrentDictionary<Type, IModService> _modServices = [];

        public void Register<T>(RuntimeMod mod, T implementation)
        {
            ArgumentNullException.ThrowIfNull(implementation);

            var type = typeof(T);
            if (AssemblyLoadContext.GetLoadContext(typeof(T).Assembly) != mod.ModAssemblyLoadContext)
                throw new InvalidOperationException("The service contract must be defined in the registering mod's assembly load context.");

            var modService = new ModService<T>()
            {
                Implementation = implementation
            };

            if (!_modServices.TryAdd(type, modService))
                throw new InvalidOperationException($"A service for type '{type}' was already registered.");
        }

        public T Get<T>()
        {
            if (!_modServices.TryGetValue(typeof(T), out var service))
            {
                throw new InvalidOperationException($"No service for type '{typeof(T)}' has been registered.");
            }

            if (service is not ModService<T> modService)
                throw new InvalidOperationException($"The registered service for type '{typeof(T)}' has an unexpected type.");

            return modService.Implementation;
        }

        private interface IModService { }

        private class ModService<T> : IModService
        {
            public required T Implementation { get; init; }

        }
    }
}
