using HarmonyLib;
using KSA;
using StarMap.API.Services;
using StarMap.Core.Services;

namespace StarMap.Core.Patches
{
    internal static class ModManifestIndexFix
    {
        public static bool AfterPrepareAll(ILogger logger)
        {
            try
            {
                var mods = ModLibrary.Manifest?.Mods;
                if (mods is not null)
                {
                    for (var index = 0; index < mods.Count; index++)
                        mods[index].Index = index;
                }
            }
            catch (Exception exception)
            {
                logger.Warning($"StarMap - Mod settings index fix failed: {exception}");
            }

            return true;
        }
    }
}
