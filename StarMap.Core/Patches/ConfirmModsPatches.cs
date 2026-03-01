using HarmonyLib;
using KSA;

namespace StarMap.Core.Patches
{
    [HarmonyPatch(typeof(ConfirmModPopup))]
    internal sealed class ConfirmModsPatches
    {
        public static bool HasEnabledMods { get; private set; } = false;

        public static ModEntry[] ModsToConfirm { get; private set; } = [];

        /*[HarmonyPatch(MethodType.Constructor)]
        [HarmonyPrefix]
        public static bool BeforeConstructor()
        {
            ModsToConfirm = ModLibrary.Manifest.Mods.Where(modEntry => modEntry.New).ToArray();
            return true;
        }*/

        /*[HarmonyPatch("Show", MethodType.Getter)]
        [HarmonyPrefix]
        public static void BeforeShowGetter()
        {
            if (HasEnabledMods) return;

            HasEnabledMods = ModsToConfirm.Any(modEntry => modEntry.Enabled);
        }*/
    }
}
