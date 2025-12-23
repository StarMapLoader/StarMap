using KSA;
using StarMap.API;
using StarMap.Core.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Tomlet;

namespace StarMap.Core.ModRepository
{
    internal class RuntimeMod
    {
        private static readonly string _rootContentPath = Path.Combine(["Content"]);

        public required string ModId { get; init; }
        public required ModAssemblyLoadContext ModAssemblyLoadContext { get; init; }
        public required Type ModType { get; init; }
        public required StarMapConfig Config { get; init; }

        public bool Initialized { get; set; } = false;
        public object? ModInstance { get; set; } = null;

        public HashSet<string> ExportedAssemblies { get; set; } = [];
        public Dictionary<RuntimeMod, HashSet<string>> Dependencies { get; set; } = [];
        public Dictionary<string, StarMapModDependency> NotLoadedModDependencies { get; set; } = [];

        public MethodInfo? BeforeMainAction { get; set; } = null;
        public MethodInfo? PrepareSystemsAction { get; set; } = null;


        public static bool TryCreateMod(ModEntry manifestEntry, AssemblyLoadContext coreALC, [NotNullWhen(true)] out RuntimeMod? runtimeMod)
        {
            runtimeMod = null;

            var modPath = Path.Combine(_rootContentPath, manifestEntry.Id);
            var modTomlPath = Path.Combine(modPath, "mod.toml");
            if (!File.Exists(modTomlPath)) return false;
            var tomlConfig = TomletMain.To<RootConfig>(File.ReadAllText(modTomlPath));
            if (tomlConfig?.StarMap is not StarMapConfig starMapConfig)
            {
                starMapConfig = new StarMapConfig
                {
                    EntryAssembly = manifestEntry.Id,
                };
            }

            var modAssemblyFile = Path.Combine(modPath, $"{starMapConfig.EntryAssembly}.dll");
            var assemblyExists = File.Exists(modAssemblyFile);

            if (!assemblyExists) return false;

            var modLoadContext = new ModAssemblyLoadContext(manifestEntry.Id, modAssemblyFile, coreALC);
            var modAssembly = modLoadContext.LoadFromAssemblyName(new AssemblyName() { Name = starMapConfig.EntryAssembly });

            var modClass = modAssembly.GetTypes().FirstOrDefault(type => type.GetCustomAttributes().Any(attr => attr.GetType().Name == typeof(StarMapModAttribute).Name));
            if (modClass is null) return false;

            runtimeMod = new RuntimeMod
            {
                ModId = manifestEntry.Id,
                ModAssemblyLoadContext = modLoadContext,
                ModType = modClass,
                Config = starMapConfig,
            };

            modLoadContext.RuntimeMod = runtimeMod;

            return true;
        }

        public bool AllDependenciesLoaded(ModRegistry modRegistry)
        {
            foreach (var dependency in Config.ModDependencies)
            {
                if (!modRegistry.TryGetMod(dependency.ModId, out var modDependency))
                {
                    NotLoadedModDependencies.Add(dependency.ModId, dependency);

                    if (!modRegistry.WaitingModsDependencyGraph.TryGetValue(dependency.ModId, out var dependents))
                    {
                        dependents = [];
                        modRegistry.WaitingModsDependencyGraph[dependency.ModId] = dependents;
                    }
                    dependents.Add(this);
                }
                else
                {
                    Dependencies.Add(modDependency, [.. CalculateUseableAssemblies(modDependency, dependency)]);
                }
            }

            if (NotLoadedModDependencies.Count > 0)
            {
                
                modRegistry.WaitingMods.Add(this);
                return false;
            }

            return true;
        }

        private static IEnumerable<string> CalculateUseableAssemblies(RuntimeMod dependency, StarMapModDependency dependencyInfo)
        {

            var hasImportedAssemblies = dependencyInfo.ImportedAssemblies.Count > 0;
            var hasExportedAssemblies = dependency.ExportedAssemblies.Count > 0;

            if (!hasImportedAssemblies && !hasExportedAssemblies)
            {
                return [dependency.Config.EntryAssembly];
            }

            if (hasImportedAssemblies && !hasExportedAssemblies)
                return dependencyInfo.ImportedAssemblies;

            if (!hasImportedAssemblies && hasExportedAssemblies)
                return dependency.ExportedAssemblies;

            return dependency.ExportedAssemblies.Intersect(dependencyInfo.ImportedAssemblies);
        }

        public bool InitializeMod(ModRegistry modRegistry)
        {
            var modObject = Activator.CreateInstance(ModType);
            if (modObject is null) return false;
            ModInstance = modObject;
            Initialized = true;

            var classMethods = ModType.GetMethods();

            foreach (var classMethod in classMethods)
            {
                var stringAttrs = classMethod.GetCustomAttributes().Select((attr) => attr.GetType().Name).Where(ModLoader.RegisteredMethodAttributes.Keys.Contains);
                foreach (var stringAttr in stringAttrs)
                {
                    var attr = ModLoader.RegisteredMethodAttributes[stringAttr];

                    if (!attr.IsValidSignature(classMethod)) continue;

                    modRegistry.AddModMethod(ModId, attr, modObject, classMethod);
                }
            }

            if (modRegistry.TryGetMod(ModId, out var modInfo) && modInfo.BeforeMainAction is MethodInfo action)
            {
                action.Invoke(modInfo.ModInstance, []);
            }

            return true;
        }

        public List<RuntimeMod> CheckForDependentMods(ModRegistry modRegistry)
        {
            List<RuntimeMod> loadableMods = [];
            if (modRegistry.WaitingModsDependencyGraph.TryGetValue(ModId, out var modDependents))
            {
                foreach (var modDependent in modDependents)
                {
                    var dependencyInfo = modDependent.NotLoadedModDependencies[ModId];
                    modDependent.Dependencies.Add(this, [.. dependencyInfo.ImportedAssemblies]);
                    if (modDependent.NotLoadedModDependencies.Remove(ModId) && modDependent.NotLoadedModDependencies.Count == 0)
                    {
                        modRegistry.WaitingMods.Remove(modDependent);
                        loadableMods.Add(modDependent);
                    }
                }
                modRegistry.WaitingModsDependencyGraph.Remove(ModId);
            }
            return loadableMods;
        }
    }
}
