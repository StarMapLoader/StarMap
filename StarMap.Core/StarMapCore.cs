using Microsoft.Extensions.DependencyInjection;
using StarMap.API.Registries;
using StarMap.API.Services;
using StarMap.Core.API;
using StarMap.Core.ModRepository;
using StarMap.Core.Patches;
using StarMap.Core.Services;
using StarMap.Core.UI;
using System.Runtime.Loader;

namespace StarMap.Core
{
    internal class StarMapCore : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;

        public StarMapCore(AssemblyLoadContext coreAssemblyLoadContext)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton("Core", coreAssemblyLoadContext);
            serviceCollection.AddSingleton<IServiceProviderService, ServiceProviderService>();
            serviceCollection.AddSingleton<IPatchManager, PatchManager>();
            serviceCollection.AddSingleton<IModsLoader, ModsLoader>();
            serviceCollection.AddSingleton<IStarMapPatches, StarMapPatches>();
            serviceCollection.AddSingleton<IModMenu, ModMenu>();
            serviceCollection.AddSingleton<IMediator, Mediator>();
            serviceCollection.AddSingleton<IModServices, ModServices>();

            serviceCollection.AddScoped<IModContext, ModContext>();
            serviceCollection.AddScoped<IPatchService, PatchService>();
            serviceCollection.AddScoped<IModMenuService, ModMenuService>();
            serviceCollection.AddScoped<IEventBus, EventBus>();
            serviceCollection.AddScoped<IModServiceRegistry, ModServiceRegistry>();

            serviceCollection.AddSingleton<ICoreService, CoreService>();

            _serviceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                _serviceProvider.GetRequiredService<ICoreService>().Init();
            }
            catch
            {
                ((IDisposable)_serviceProvider).Dispose();
                throw;
            }

        }

        public void Dispose()
        {
            ((IDisposable)_serviceProvider).Dispose();
        }




    }
}