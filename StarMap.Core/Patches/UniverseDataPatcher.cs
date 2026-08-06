using System.IO;
using HarmonyLib;
using KSA;
using StarMap.API;

namespace StarMap.Core.Patches
{
    /// <summary>
    /// Tells mods when a save is written or read, and where.
    ///
    /// A save is a directory, so a mod that keeps per-save state can put a file beside
    /// universe.xml. Without a hook the only way to notice is to poll that file's timestamp,
    /// which leaves a window: save and load inside it and the mod writes nothing for that save,
    /// then writes over it afterwards.
    /// </summary>
    [HarmonyPatch(typeof(UniverseData))]
    internal static class UniverseDataPatcher
    {
        private const string WriteToMethodName = "WriteTo";
        private const string LoadFromMethodName = "LoadFrom";

        // WriteTo is overloaded; the DirectoryInfo one is the save on disk.
        [HarmonyPatch(WriteToMethodName, [typeof(DirectoryInfo)])]
        [HarmonyPostfix]
        public static void AfterWriteTo(DirectoryInfo directory)
        {
            var methods = StarMapCore.Instance?.Loader.ModRegistry.Get<StarMapAfterSaveAttribute>() ?? [];

            foreach (var (_, @object, method) in methods)
            {
                method.Invoke(@object, [directory]);
            }
        }

        [HarmonyPatch(LoadFromMethodName)]
        [HarmonyPostfix]
        public static void AfterLoadFrom(UncompressedSave uncompressedSave)
        {
            var methods = StarMapCore.Instance?.Loader.ModRegistry.Get<StarMapAfterLoadAttribute>() ?? [];

            foreach (var (_, @object, method) in methods)
            {
                method.Invoke(@object, [uncompressedSave.Directory]);
            }
        }
    }
}
