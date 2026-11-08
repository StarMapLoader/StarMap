using KSA;
using Microsoft.Extensions.DependencyInjection;
using StarMap.API;
using StarMap.API.Services;
using StarMap.Core.Config;
using StarMap.Core.Patches;
using StarMap.Core.Services;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using Tomlet;

namespace StarMap.Core.ModRepository
{
    internal class RuntimeMod : IDisposable
    {
        private static readonly string _rootContentPath = Path.Combine(["Content"]);

        public required string ModId { get; init; }
        public required ModAssemblyLoadContext ModAssemblyLoadContext { get; init; }
        public required StarMapConfig Config { get; init; }
        public required Assembly ModAssembly { get; init; }

        public bool Initialized { get; set; } = false;
        public Type? ModType { get; set; }
        public object? ModInstance { get; set; } = null;

        public HashSet<string> ExportedAssemblies { get; set; } = [];
        public Dictionary<RuntimeMod, HashSet<string>> Dependencies { get; } = [];
        public Dictionary<string, StarMapModDependency> NotLoadedModDependencies { get; } = [];
        public HashSet<IDisposable> Patches { get; } = [];

        public MethodInfo? BeforeMainAction { get; set; } = null;
        public MethodInfo? PrepareSystemsAction { get; set; } = null;


        public static bool TryCreateMod(ModEntry manifestEntry, AssemblyLoadContext coreALC, [NotNullWhen(true)] out RuntimeMod? runtimeMod)
        {
            runtimeMod = null;

            var modPath = Path.Combine(_rootContentPath, manifestEntry.Id);
            var modTomlPath = Path.Combine(modPath, "mod.toml");
            if (!File.Exists(modTomlPath))
            {
                modPath = Path.Combine(ModLibrary.LocalModsFolderPath, manifestEntry.Id);
                modTomlPath = Path.Combine(modPath, "mod.toml");
                if (!File.Exists(modTomlPath)) return false;
            }
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

            runtimeMod = new RuntimeMod
            {
                ModId = manifestEntry.Id,
                ModAssemblyLoadContext = modLoadContext,
                ModAssembly = modAssembly,
                Config = starMapConfig,
                ExportedAssemblies = [.. starMapConfig.ExportedAssemblies],
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

        public bool InitializeMod(ModRegistry modRegistry, IPatchManager patchManager, IServiceProviderService serviceProviderService)
        {
            var modClass = ModAssembly.GetTypes().FirstOrDefault(type => type.GetCustomAttributes().Any(attr => attr.GetType().Name == typeof(StarMapModAttribute).Name));
            if (modClass is null) return false;

            ModType = modClass;

            var services = serviceProviderService.CreateModDependencies(this);
            var modObject = ActivatorUtilities.CreateInstance(services, ModType);
            if (modObject is null) return false;
            ModInstance = modObject;
            Initialized = true;

            var classMethods = ModType.GetMethods();

            foreach (var classMethod in classMethods)
            {
                var stringAttrs = classMethod.GetCustomAttributes().Select((attr) => attr.GetType().Name).Where(ModsLoader.RegisteredMethodAttributes.Keys.Contains);
                foreach (var stringAttr in stringAttrs)
                {
                    var attr = ModsLoader.RegisteredMethodAttributes[stringAttr];

                    if (!attr.IsValidSignature(classMethod)) continue;

                    switch (attr)
                    {
                        case StarMapBeforeMainAttribute:
                        case StarMapUnloadAttribute:
                            modRegistry.AddModMethod(ModId, attr, modObject, classMethod);
                            break;
                        case StarMapAllModsLoadedAttribute:
                            var allModsLoadedPatch = patchManager.RegisterStaticVoidPatch(IPatchService.PatchType.Postfix, this, typeof(KSA.ModLibrary).GetMethod(nameof(ModLibrary.LoadAll))!,
                                args =>
                                {
                                    classMethod.Invoke(modObject, []);
                                    return true;
                                });
                            Patches.Add(allModsLoadedPatch);
                            break;

                        case StarMapImmediateLoadAttribute:
                            // KSA 2026.11.10.5801 removed Mod.PrepareSystems, and ModLibrary.PrepareAll now calls Mod.LoadSettings at its place.
                            var immediateLoadTarget = typeof(KSA.Mod).GetMethod("PrepareSystems") ?? typeof(KSA.Mod).GetMethod(nameof(Mod.LoadSettings));
                            if (immediateLoadTarget is null)
                            {
                                Console.WriteLine($"StarMap warning: 'StarMap - {ModId} uses StarMapImmediateLoad, but KSA has no method to hook it to'");
                                break;
                            }

                            var immediateLoadPatch = patchManager.RegisterInstanceVoidPatch<Mod>(IPatchService.PatchType.Prefix, this, immediateLoadTarget,
                                (args, mod) =>
                                {
                                    if (mod.Id.Equals(ModId))
                                        classMethod.Invoke(modObject, [mod]);

                                    return true;
                                });
                            Patches.Add(immediateLoadPatch);
                            break;
                        case StarMapAfterOnFrameAttribute:
                            var afterOnFramepatch = patchManager.RegisterInstanceVoidPatch<Program>(IPatchService.PatchType.Postfix, this, typeof(Program).GetMethod("OnFrame", BindingFlags.NonPublic | BindingFlags.Instance)!,
                                (args, mod) =>
                                {
                                    classMethod.Invoke(modObject, [args[0], args[1]]);

                                    return true;
                                });
                            Patches.Add(afterOnFramepatch);
                            break;
                        case StarMapBeforeGuiAttribute:
                            var beforeGuiPatch = patchManager.RegisterInstanceVoidPatch<Program>(IPatchService.PatchType.Prefix, this, typeof(Program).GetMethod("OnDrawUiFrame", BindingFlags.NonPublic | BindingFlags.Instance)!,
                                (args, mod) =>
                                {
                                    classMethod.Invoke(modObject, [args[0]]);

                                    return true;
                                });
                            Patches.Add(beforeGuiPatch);
                            break;
                        case StarMapAfterGuiAttribute:
                            var afterGuiPatch = patchManager.RegisterInstanceVoidPatch<Program>(IPatchService.PatchType.Postfix, this, typeof(Program).GetMethod("OnDrawUiViewports", BindingFlags.NonPublic | BindingFlags.Instance)!,
                                (args, mod) =>
                                {
                                    classMethod.Invoke(modObject, [args[0]]);

                                    return true;
                                });
                            Patches.Add(afterGuiPatch);
                            break;
                    }
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
                    modDependent.Dependencies.Add(this, [.. CalculateUseableAssemblies(this, dependencyInfo)]);
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

        public void Dispose()
        {
            foreach (var patch in Patches.ToArray())
            {
                patch.Dispose();
            }

            Patches.Clear();
        }
    }
}
