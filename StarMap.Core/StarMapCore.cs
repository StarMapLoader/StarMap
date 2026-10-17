using Microsoft.Extensions.DependencyInjection;
using StarMap.API.Interfaces;
using StarMap.Core.API;
using StarMap.Core.ModRepository;
using StarMap.Core.Patches;
using StarMap.Types;
using System.Runtime.Loader;

namespace StarMap.Core
{
    internal interface ICoreService
    {
        AssemblyLoadContext CoreAssemblyLoadContext { get; }
        IServiceProvider CreateModDependencies(RuntimeMod mod);
    }

    internal class StarMapCore : IStarMapCore, ICoreService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Lock _lock = new();
        private readonly List<IServiceScope> _modScopes = [];

        private readonly AssemblyLoadContext _coreAssemblyLoadContext;

        public AssemblyLoadContext CoreAssemblyLoadContext => _coreAssemblyLoadContext;

        public StarMapCore(AssemblyLoadContext coreAssemblyLoadContext)
        {
            _coreAssemblyLoadContext = coreAssemblyLoadContext;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ICoreService>(this);
            serviceCollection.AddSingleton<IPatchManager, PatchManager>();
            serviceCollection.AddSingleton<IModsLoader, ModsLoader>();
            serviceCollection.AddSingleton<IStarMapPatches, StarMapPatches>();

            serviceCollection.AddScoped<IModContext, ModContext>();
            serviceCollection.AddScoped<IPatchHelper, PatchHelper>();

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public void Init()
        {
            _serviceProvider.GetRequiredService<IStarMapPatches>().Init();
            _serviceProvider.GetRequiredService<IModsLoader>().Init();
        }

        public void DeInit()
        {
            lock (_lock)
            {
                foreach(var scope in _modScopes) 
                {
                    scope.Dispose();
                }
            }

            _serviceProvider.GetRequiredService<IModsLoader>().Dispose();
        }

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

        
    }
}