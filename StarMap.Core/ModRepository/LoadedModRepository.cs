using KSA;
using StarMap.API;
using System.Reflection;
using System.Runtime.Loader;

namespace StarMap.Core.ModRepository
{
    internal class LoadedModRepository : IDisposable
    {
        private readonly AssemblyLoadContext _coreAssemblyLoadContext;

        private readonly Dictionary<string, StarMapMethodAttribute> _registeredMethodAttributes = [];
        private readonly Dictionary<string, bool> _attemptedMods = [];
        private readonly Dictionary<string, ModAssemblyLoadContext> _modLoadContexts = [];

        private readonly ModRegistry _mods = new();
        public ModRegistry Mods => _mods;

        private (string attributeName, StarMapMethodAttribute attribute)? ConvertAttributeType(Type attrType)
        {
            if ((Activator.CreateInstance(attrType) as StarMapMethodAttribute) is not StarMapMethodAttribute attrObject) return null;
            return (attrType.Name, attrObject);
        }

        public LoadedModRepository(AssemblyLoadContext coreAssemblyLoadContext)
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

                if (!LoadMod(mod.Id, modPath))
                {
                    _attemptedMods[mod.Id] = false;
                    continue;
                }

                if (_mods.GetBeforeMainAction(mod.Id) is (object @object, MethodInfo method))
                {
                    method.Invoke(@object, []);
                }
                _attemptedMods[mod.Id] = true;
            }
        }

        public void ModPrepareSystems(Mod mod)
        {
            if (!_attemptedMods.TryGetValue(mod.Id, out var succeeded))
            {
                succeeded = LoadMod(mod.Id, mod.DirectoryPath);
            }

            if (!succeeded) return;

            if (_mods.GetPrepareSystemsAction(mod.Id) is (object @object, MethodInfo method))
            {
                method.Invoke(@object, [mod]);
            }
        }

        private bool LoadMod(string modId, string modDirectory)
        {
            var fullPath = Path.GetFullPath(modDirectory);
            var modAssemblyFile = Path.Combine(fullPath, $"{modId}.dll");
            var assemblyExists = File.Exists(modAssemblyFile);

            if (!assemblyExists) return false;

            var modLoadContext = new ModAssemblyLoadContext(modId, modDirectory, _coreAssemblyLoadContext);
            var modAssembly = modLoadContext.LoadFromAssemblyName(new AssemblyName() { Name = modId });

            var modClass = modAssembly.GetTypes().FirstOrDefault(type => type.GetCustomAttributes().Any(attr => attr.GetType().Name == typeof(StarMapModAttribute).Name));
            if (modClass is null) return false;

            var modObject = Activator.CreateInstance(modClass);
            if (modObject is null) return false;

            _modLoadContexts.Add(modId, modLoadContext);

            var classMethods = modClass.GetMethods();
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

                    _mods.Add(modId, attr, modObject, classMethod);
                }
            }

            Console.WriteLine($"StarMap - Loaded mod: {modId} from {modAssemblyFile}");
            return true;
        }

        public void OnAllModsLoaded()
        {
            foreach (var (_, @object, method) in _mods.Get<StarMapAllModsLoadedAttribute>())
            {
                method.Invoke(@object, []);
            }
        }

        public void Dispose()
        {
            foreach (var (_, @object, method) in _mods.Get<StarMapUnloadAttribute>())
            {
                method.Invoke(@object, []);
            }

            _mods.Dispose();
        }
    }
}
