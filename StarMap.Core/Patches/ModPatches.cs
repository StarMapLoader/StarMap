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
            StarMapCore.Instance?.LoadedMods.ModPrepareSystems(__instance);
        }
    }
}
