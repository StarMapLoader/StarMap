using KSA;
using Microsoft.Extensions.DependencyInjection;
using StarMap.API;
using StarMap.Core.Patches;
using StarMap.Core.Services;
using System.Reflection;
using System.Runtime.Loader;

namespace StarMap.Core.ModRepository
{
    internal interface IModsLoader : IDisposable
    {
        void Init();
    }

    internal sealed class ModsLoader : IModsLoader
    {
        private readonly AssemblyLoadContext _coreAssemblyLoadContext;
        private readonly IPatchManager _patchManager;
        private readonly IServiceProviderService _serviceProviderService;
        private readonly ILogger _logger;

        public static Dictionary<string, StarMapMethodAttribute> RegisteredMethodAttributes = [];


        private readonly ModRegistry _modRegistry = new();
        public ModRegistry ModRegistry => _modRegistry;

        private (string attributeName, StarMapMethodAttribute attribute)? ConvertAttributeType(Type attrType)
        {
            if ((Activator.CreateInstance(attrType) as StarMapMethodAttribute) is not StarMapMethodAttribute attrObject) return null;
            return (attrType.Name, attrObject);
        }

        public ModsLoader([FromKeyedServices("Core")] AssemblyLoadContext coreAssemblyLoadContext, IPatchManager patchManager, IServiceProviderService serviceProviderService, ILogger logger)
        {
            _coreAssemblyLoadContext = coreAssemblyLoadContext;
            _patchManager = patchManager;
            _serviceProviderService = serviceProviderService;
            _logger = logger;

            var coreAssembly = typeof(StarMapModAttribute).Assembly;

            RegisteredMethodAttributes = coreAssembly
                .GetTypes()
                .Where(t =>
                    typeof(StarMapMethodAttribute).IsAssignableFrom(t) &&
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.GetCustomAttribute<AttributeUsageAttribute>()?.ValidOn.HasFlag(AttributeTargets.Method) == true
                )
                .Select(ConvertAttributeType)
                .OfType<(string attributeName, StarMapMethodAttribute attribute)>()
                .ToDictionary();
        }

        public void Init()
        {
            PrepareMods();
        }

        private void PrepareMods()
        {
            var loadedManifest = ModLibrary.PrepareManifest();

            if (!loadedManifest) return;

            var mods = ModLibrary.Manifest.Mods;
            if (mods is null) return;

            foreach (var mod in mods)
            {
                if (!mod.Enabled)
                {
                    _logger.Information($"StarMap - Not loading mod: {mod.Id} because it is disabled in manifest");
                    continue;
                }

                if (!RuntimeMod.TryCreateMod(mod, _coreAssemblyLoadContext, out var runtimeMod))
                    continue;

                ModRegistry.Add(runtimeMod);

                if (!runtimeMod.AllDependenciesLoaded(ModRegistry))
                {
                    _logger.Information($"StarMap - Delaying load of mod: {runtimeMod.ModId} due to missing dependencies: {string.Join(", ", runtimeMod.NotLoadedModDependencies.Keys)}");
                    continue;
                }

                if (!runtimeMod.InitializeMod(ModRegistry, _patchManager, _serviceProviderService))
                {
                    _logger.Warning($"StarMap - Failed to initialize mod: {runtimeMod.ModId} from manifest");
                    continue;
                }

                _logger.Information($"StarMap - Loaded mod: {runtimeMod.ModId} from manifest");

                var dependentMods = runtimeMod.CheckForDependentMods(ModRegistry);

                foreach (var dependentMod in dependentMods)
                {
                    if (dependentMod.InitializeMod(ModRegistry, _patchManager, _serviceProviderService))
                    {
                        _logger.Information($"StarMap - Loaded mod: {dependentMod.ModId} after loading {runtimeMod.ModId}");
                    }
                    else
                    {
                        _logger.Warning($"StarMap - Failed to load mod: {dependentMod.ModId} after loading {runtimeMod.ModId}");
                    }
                }
            }

            TryLoadWaitingMods();
        }

        private void TryLoadWaitingMods()
        {
            var loadedMod = true;

            while (ModRegistry.WaitingModsDependencyGraph.Count > 0 && loadedMod)
            {
                loadedMod = false;
                foreach (var waitingMod in ModRegistry.WaitingMods)
                {
                    if (waitingMod.NotLoadedModDependencies.Count == 0 || waitingMod.NotLoadedModDependencies.Values.All(dependencyInfo => dependencyInfo.Optional))
                    {
                        loadedMod = true;
                        ModRegistry.WaitingMods.Remove(waitingMod);

                        if (waitingMod.InitializeMod(ModRegistry, _patchManager, _serviceProviderService))
                        {
                            _logger.Information($"StarMap - Loaded mod: {waitingMod.ModId} after all mods were loaded, not loaded optional mods: {string.Join(",", waitingMod.NotLoadedModDependencies.Values.Select(mod => mod.ModId))}");
                        }
                        else
                        {
                            _logger.Warning($"StarMap - Failed to load mod:{waitingMod.ModId} after all mods were loaded, not loaded optional mods: {string.Join(",", waitingMod.NotLoadedModDependencies.Values.Select(mod => mod.ModId))}");
                        }
                        waitingMod.NotLoadedModDependencies.Clear();
                    }
                }
            }

            if (ModRegistry.WaitingMods.Count > 0)
            {
                foreach (var waitingMod in ModRegistry.WaitingMods)
                {
                    _logger.Warning($"StarMap - Failed to load mod:{waitingMod.ModId} after all mods were loaded, missing mods (some may be optional): {string.Join(",", waitingMod.NotLoadedModDependencies.Values.Select(mod => mod.ModId))}");
                }
                ModRegistry.WaitingMods.Clear();
            }
        }

        public void Dispose()
        {
            foreach (var (_, @object, method) in _modRegistry.Get<StarMapUnloadAttribute>())
            {
                method.Invoke(@object, []);
            }

            _modRegistry.Dispose();
        }
    }
}
