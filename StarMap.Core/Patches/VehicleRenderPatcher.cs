using HarmonyLib;
using Brutal.VulkanApi;
using KSA;
using StarMap.API;

namespace StarMap.Core.Patches
{
    /// <summary>
    /// Patches Vehicle.ProcessGroundTrackMap to allow mods to perform GPU rendering
    /// during the Vulkan render pass.
    /// </summary>
    [HarmonyPatch(typeof(Vehicle))]
    internal static class VehicleRenderPatcher
    {
        private const string ProcessGroundTrackMapMethodName = "ProcessGroundTrackMap";

        /// <summary>
        /// Called after Vehicle.ProcessGroundTrackMap, giving mods access to the CommandBuffer
        /// for GPU rendering operations like PlanetMapExporter.
        /// </summary>
        [HarmonyPatch(ProcessGroundTrackMapMethodName)]
        [HarmonyPostfix]
        public static void AfterProcessGroundTrackMap(
            CommandBuffer commandBuffer, 
            Celestial nearbyCelestial, 
            Viewport viewport, 
            int frameIndex)
        {
            var methods = StarMapCore.Instance?.LoadedMods.Mods.Get<StarMapOnRenderAttribute>() ?? [];

            foreach (var (_, @object, method) in methods)
            {
                method.Invoke(@object, new object[] { commandBuffer, nearbyCelestial, viewport, frameIndex });
            }
        }
    }
}
