using Microsoft.Extensions.DependencyInjection;
using StarMap.API.Interfaces;
using StarMap.Core.API;
using StarMap.Core.ModRepository;
using StarMap.Core.Patches;
using StarMap.Types;
using System.Runtime.Loader;

namespace StarMap.Core
{
    internal interface ICoreSerivce
    {
        AssemblyLoadContext CoreAssemblyLoadContext { get; }
        IServiceProvider CreadeModDependencies(RuntimeMod mod);
    }

    internal class StarMapCore : IStarMapCore, ICoreSerivce
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly AssemblyLoadContext _coreAssemblyLoadContext;

        public AssemblyLoadContext CoreAssemblyLoadContext => _coreAssemblyLoadContext;

        public StarMapCore(AssemblyLoadContext coreAssemblyLoadContext)
        {
            _coreAssemblyLoadContext = coreAssemblyLoadContext;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ICoreSerivce>(this);
            serviceCollection.AddSingleton<IPatchManager, PatchManager>();
            serviceCollection.AddSingleton<IModsLoader, ModsLoader>();

            serviceCollection.AddScoped<IModContext, ModContext>();
            serviceCollection.AddScoped<IPatchHelper, PatchHelper>();

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public void Init()
        {
            _serviceProvider.GetRequiredService<IModsLoader>().Init();
        }

        public void DeInit()
        {
            _serviceProvider.GetRequiredService<IModsLoader>().Dispose();
        }

        public IServiceProvider CreadeModDependencies(RuntimeMod mod)
        {
            var scope = _serviceProvider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IModContext>().Mod = mod;
            return scope.ServiceProvider;
        }

        
    }
}