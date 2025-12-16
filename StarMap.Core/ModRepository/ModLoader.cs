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

        private readonly Dictionary<string, StarMapMethodAttribute> _registeredMethodAttributes = [];
        private readonly Dictionary<string, List<ModInformation>> _modDependencyGraph = [];
        private readonly HashSet<ModInformation> _waitingMods = [];

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

            _registeredMethodAttributes = coreAssembly
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
                var modPath = Path.Combine(path, mod.Id);
                var starMapConfig = TomletMain.To<RootConfig>(File.ReadAllText(Path.Combine(modPath, "mod.toml")));
                if (starMapConfig.StarMap is null)
                    continue;

                if (!LoadMod(mod.Id, modPath, starMapConfig.StarMap))
                {
                    continue;
                }

                if (_modRegistry.TryGetMod(mod.Id, out var modInfo) && modInfo.BeforeMainAction is MethodInfo action)
                {
                    action.Invoke(modInfo.ModInstance, []);
                }
            }

            var loadedMod = true;

            while (_modDependencyGraph.Count > 0 && loadedMod)
            {
                loadedMod = false;
                foreach (var waitingMod in _waitingMods)
                {
                   if (waitingMod.NotLoadedModDependencies.Count == 0 || waitingMod.NotLoadedModDependencies.Values.All(dependencyInfo => dependencyInfo.Optional))
                   {
                        loadedMod = true;
                        _waitingMods.Remove(waitingMod);

                        if (InitializeMod(waitingMod))
                        {
                            Console.WriteLine($"StarMap - Loaded mod: {waitingMod.ModId} after all mods were loaded, not loaded optional mods: {string.Join(",", waitingMod.NotLoadedModDependencies.Values.Select(mod => mod.ModId))}");
                        }
                        else
                        {
                            Console.WriteLine($"StarMap - Failed to load mod:{ waitingMod.ModId} after all mods were loaded, not loaded optional mods: {string.Join(",", waitingMod.NotLoadedModDependencies.Values.Select(mod => mod.ModId))}");
                        }
                        waitingMod.NotLoadedModDependencies.Clear();
                    }
                }
            }

            if (_waitingMods.Count > 0)
            {
                foreach (var waitingMod in _waitingMods)
                {
                    Console.WriteLine($"StarMap - Failed to load mod:{waitingMod.ModId} after all mods were loaded, missing mods (some may be optional): {string.Join(",", waitingMod.NotLoadedModDependencies.Values.Select(mod => mod.ModId))}");
                }
                _waitingMods.Clear();
            }
        }

        private bool LoadMod(string modId, string modDirectory, StarMapConfig config)
        {
            var fullPath = Path.GetFullPath(modDirectory);
            var modAssemblyFile = Path.Combine(fullPath, $"{modId}.dll");
            var assemblyExists = File.Exists(modAssemblyFile);

            if (!assemblyExists) return false;

            var modLoadContext = new ModAssemblyLoadContext(modId, modDirectory, _coreAssemblyLoadContext);
            var modAssembly = modLoadContext.LoadFromAssemblyName(new AssemblyName() { Name = modId });

            var modClass = modAssembly.GetTypes().FirstOrDefault(type => type.GetCustomAttributes().Any(attr => attr.GetType().Name == typeof(StarMapModAttribute).Name));
            if (modClass is null) return false;

            var modInfo = new ModInformation() {
                ModId = modId,
                ModAssemblyLoadContext = modLoadContext,
                ModType = modClass,
                Config = config,
            };

            foreach(var exportedAssembly in config.ExportedAssemblies)
            {
                modInfo.ExportedAssemblies.Add(exportedAssembly);
            }

            modLoadContext.ModInfo = modInfo;
            _modRegistry.Add(modInfo);

            foreach (var dependency in config.ModDependencies)
            {
                if (!_modRegistry.TryGetMod(dependency.ModId, out var modDependency))
                {
                    modInfo.NotLoadedModDependencies.Add(dependency.ModId, dependency);
                    if (!_modDependencyGraph.TryGetValue(dependency.ModId, out var dependents))
                    {
                        dependents = [];
                        _modDependencyGraph[dependency.ModId] = dependents;
                    }
                    dependents.Add(modInfo);
                }
                else
                {
                    modInfo.Dependencies.Add(modDependency, [.. dependency.ImportedAssemblies]);
                }
            }

            if (modInfo.NotLoadedModDependencies.Count > 0)
            {
                Console.WriteLine($"StarMap - Delaying load of mod: {modInfo.ModId} due to missing dependencies: {string.Join(", ", modInfo.NotLoadedModDependencies.Keys)}");
                _waitingMods.Add(modInfo);
                return false;
            }

            if (!InitializeMod(modInfo)) 
            {
                Console.WriteLine($"StarMap - Failed to initialize mod: {modInfo.ModId} from {modAssemblyFile}");
                return false;
            }

            Console.WriteLine($"StarMap - Loaded mod: {modInfo.ModId} from {modAssemblyFile}");

            if (_modDependencyGraph.TryGetValue(modInfo.ModId, out var modDependents))
            {
                foreach(var modDependent in modDependents)
                {
                    var dependencyInfo = modDependent.NotLoadedModDependencies[modInfo.ModId];
                    modDependent.Dependencies.Add(modInfo, [.. dependencyInfo.ImportedAssemblies]);
                    if (modDependent.NotLoadedModDependencies.Remove(modInfo.ModId) && modDependent.NotLoadedModDependencies.Count == 0)
                    {
                        _waitingMods.Remove(modDependent);
                        if (InitializeMod(modDependent))
                        {
                            Console.WriteLine($"StarMap - Loaded mod: {modDependent.ModId} after loading {modInfo.ModId}");
                        }
                        else
                        {
                            Console.WriteLine($"StarMap - Failed to load mod: {modDependent.ModId} after loading {modInfo.ModId}");
                        }
                    }
                }
                _modDependencyGraph.Remove(modInfo.ModId);
            }

            return true;
        }

        private bool InitializeMod(ModInformation modInfo)
        {
            var modObject = Activator.CreateInstance(modInfo.ModType);
            if (modObject is null) return false;
            modInfo.ModInstance = modObject;
            modInfo.Initialized = true;

            var classMethods = modInfo.ModType.GetMethods();
            var immediateLoadMethods = new List<MethodInfo>();

            foreach (var classMethod in classMethods)
            {
                var stringAttrs = classMethod.GetCustomAttributes().Select((attr) => attr.GetType().Name).Where(_registeredMethodAttributes.Keys.Contains);
                foreach (var stringAttr in stringAttrs)
                {
                    var attr = _registeredMethodAttributes[stringAttr];

                    if (!attr.IsValidSignature(classMethod)) continue;

                    if (attr.GetType() == typeof(StarMapImmediateLoadAttribute))
                    {
                        immediateLoadMethods.Add(classMethod);
                    }

                    _modRegistry.AddModMethod(modInfo.ModId, attr, modObject, classMethod);
                }
            }

            foreach (var assembly in modInfo.Config?.ExportedAssemblies ?? [])
                modInfo.ExportedAssemblies.Add(assembly);

            return true;
        }

        public void ModPrepareSystems(Mod mod)
        {
            if (_modRegistry.TryGetMod(mod.Id, out var modInfo) && modInfo.PrepareSystemsAction is MethodInfo action)
            {
                action.Invoke(modInfo.ModInstance, [mod]);
            }
        }

        public void OnAllModsLoaded()
        {
            foreach (var (_, @object, method) in _modRegistry.Get<StarMapAllModsLoadedAttribute>())
            {
                method.Invoke(@object, []);
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
