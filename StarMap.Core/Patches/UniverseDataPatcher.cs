using System.IO;
using HarmonyLib;
using KSA;
using StarMap.API;

namespace StarMap.Core.Patches
{
    /// <summary>
    /// Tells mods when a save is written or read, and which directory it is in.
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
