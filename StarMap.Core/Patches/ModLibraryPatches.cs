using HarmonyLib;
using KSA;
using StarMap.API;
using StarMap.Core.ModRepository;

namespace StarMap.Core.Patches
{
    [HarmonyPatch(typeof(ModLibrary))]
    internal class ModLibraryPatches
    {
        [HarmonyPatch(nameof(ModLibrary.LoadAll))]
        [HarmonyPostfix]
        public static void AfterLoad()
        {
            var modRegistry = StarMapCore.Instance?.Loader.ModRegistry;
            if (modRegistry is not ModRegistry registry) return;

            foreach (var (_, @object, method) in registry.Get<StarMapAllModsLoadedAttribute>())
            {
                method.Invoke(@object, []);
            }
        }
    }
}
