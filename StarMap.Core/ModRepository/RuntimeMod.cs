using KSA;
using Microsoft.Extensions.DependencyInjection;
using StarMap.API;
using StarMap.API.Interfaces;
using StarMap.Core.Config;
using StarMap.Core.Patches;
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
        public required StarMapConfig Config { get; init; }
        public required Assembly ModAssembly { get; init; }

        public bool Initialized { get; set; } = false;
        public Type? ModType { get; set; }
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

        public bool InitializeMod(ModRegistry modRegistry, ICoreService core, IPatchManager patchManager)
        {
            var modClass = ModAssembly.GetTypes().FirstOrDefault(type => type.GetCustomAttributes().Any(attr => attr.GetType().Name == typeof(StarMapModAttribute).Name));
            if (modClass is null) return false;

            ModType = modClass;

            var services = core.CreateModDependencies(this);
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
                            patchManager.RegisterStaticVoidPatch(IPatchService.PatchType.Postfix, this, typeof(KSA.ModLibrary).GetMethod(nameof(ModLibrary.LoadAll))!, 
                                args =>
                                {
                                classMethod.Invoke(modObject, args);
                                return true;
                            });
                            break;

                        case StarMapImmediateLoadAttribute:
                            patchManager.RegisterInstanceVoidPatch<Mod>(IPatchService.PatchType.Prefix, this, typeof(KSA.Mod).GetMethod(nameof(Mod.PrepareSystems))!,
                                (args, mod) =>
                                {
                                    if (mod.Id.Equals(ModId))
                                        classMethod.Invoke(modObject, [mod]);

                                    return true;
                                });
                            break;
                        case StarMapAfterOnFrameAttribute:
                            patchManager.RegisterInstanceVoidPatch<Program>(IPatchService.PatchType.Postfix, this, typeof(Program).GetMethod("OnFrame")!,
                                (args, mod) =>
                                {
                                    classMethod.Invoke(modObject, [args[0], args[1]]);

                                    return true;
                                });
                            break;
                        case StarMapBeforeGuiAttribute:
                            patchManager.RegisterInstanceVoidPatch<Program>(IPatchService.PatchType.Prefix, this, typeof(Program).GetMethod("OnDrawUiFrame", BindingFlags.NonPublic | BindingFlags.Instance)!,
                                (args, mod) =>
                                {
                                    classMethod.Invoke(modObject, [args[0]]);

                                    return true;
                                });
                            break;
                        case StarMapAfterGuiAttribute:
                            patchManager.RegisterInstanceVoidPatch<Program>(IPatchService.PatchType.Postfix, this, typeof(Program).GetMethod("OnDrawUiViewports", BindingFlags.NonPublic | BindingFlags.Instance)!,
                                (args, mod) =>
                                {
                                    classMethod.Invoke(modObject, [args[0]]);

                                    return true;
                                });
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
