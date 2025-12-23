using HarmonyLib;
using KSA;
using StarMap.Core.ModRepository;
using System.Reflection;

namespace StarMap.Core.Patches
{
    [HarmonyPatch(typeof(Mod))]
    internal static class ModPatches
    {
        [HarmonyPatch(nameof(Mod.PrepareSystems))]
        [HarmonyPrefix]
        public static void OnLoadMod(this Mod __instance)
        {
            var modRegistry = StarMapCore.Instance?.Loader.ModRegistry;
            if (modRegistry is not ModRegistry registry) return;

            if (registry.TryGetMod(__instance.Id, out var modInfo) && modInfo.PrepareSystemsAction is MethodInfo action)
            {
                action.Invoke(modInfo.ModInstance, [__instance]);
            }
        }
    }
}
