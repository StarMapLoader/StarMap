using KSA;
using StarMap.API;
using StarMap.Core.Config;
using System.Reflection;
using System.Runtime.Loader;
using Tomlet;

namespace StarMap.Core.ModRepository
{
    internal sealed class ModLoader : IDisposable
    {
        private readonly AssemblyLoadContext _coreAssemblyLoadContext;

        public static Dictionary<string, StarMapMethodAttribute> RegisteredMethodAttributes = [];


        private readonly ModRegistry _modRegistry = new();
        public ModRegistry ModRegistry => _modRegistry;

        private (string attributeName, StarMapMethodAttribute attribute)? ConvertAttributeType(Type attrType)
        {
            if ((Activator.CreateInstance(attrType) as StarMapMethodAttribute) is not StarMapMethodAttribute attrObject) return null;
            return (attrType.Name, attrObject);
        }

        public ModLoader(AssemblyLoadContext coreAssemblyLoadContext)
        {
            _coreAssemblyLoadContext = coreAssemblyLoadContext;

            Assembly coreAssembly = typeof(StarMapModAttribute).Assembly;

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

            string rootPath = "Content";
            string path = Path.Combine(new ReadOnlySpan<string>(in rootPath));

            foreach (var mod in mods)
            {
                if (!mod.Enabled)
                {
                    Console.WriteLine($"StarMap - Nod loading mod: {mod.Id} because it is disable in manifest");
                    continue;
                }

                var modPath = Path.Combine(path, mod.Id);

                if (!RuntimeMod.TryCreateMod(mod, _coreAssemblyLoadContext, out var runtimeMod))
                    continue;

                ModRegistry.Add(runtimeMod);

                if (!runtimeMod.AllDependenciesLoaded(ModRegistry))
                {
                    Console.WriteLine($"StarMap - Delaying load of mod: {runtimeMod.ModId} due to missing dependencies: {string.Join(", ", runtimeMod.NotLoadedModDependencies.Keys)}");
                    continue;
                }

                if (!runtimeMod.InitializeMod(ModRegistry))
                {
                    Console.WriteLine($"StarMap - Failed to initialize mod: {runtimeMod.ModId} from manifest");
                    continue;
                }

                Console.WriteLine($"StarMap - Loaded mod: {runtimeMod.ModId} from manifest");

                var dependentMods = runtimeMod.CheckForDependentMods(ModRegistry);
                
                foreach (var dependentMod in dependentMods)
                {
                    if (dependentMod.InitializeMod(ModRegistry))
                    {
                        Console.WriteLine($"StarMap - Loaded mod: {dependentMod.ModId} after loading {runtimeMod.ModId}");
                    }
                    else
                    {
                        Console.WriteLine($"StarMap - Failed to load mod: {dependentMod.ModId} after loading {runtimeMod.ModId}");
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

                        if (waitingMod.InitializeMod(ModRegistry))
                        {
                            Console.WriteLine($"StarMap - Loaded mod: {waitingMod.ModId} after all mods were loaded, not loaded optional mods: {string.Join(",", waitingMod.NotLoadedModDependencies.Values.Select(mod => mod.ModId))}");
                        }
                        else
                        {
                            Console.WriteLine($"StarMap - Failed to load mod:{waitingMod.ModId} after all mods were loaded, not loaded optional mods: {string.Join(",", waitingMod.NotLoadedModDependencies.Values.Select(mod => mod.ModId))}");
                        }
                        waitingMod.NotLoadedModDependencies.Clear();
                    }
                }
            }

            if (ModRegistry.WaitingMods.Count > 0)
            {
                foreach (var waitingMod in ModRegistry.WaitingMods)
                {
                    Console.WriteLine($"StarMap - Failed to load mod:{waitingMod.ModId} after all mods were loaded, missing mods (some may be optional): {string.Join(",", waitingMod.NotLoadedModDependencies.Values.Select(mod => mod.ModId))}");
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
