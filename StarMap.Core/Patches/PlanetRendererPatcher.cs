using HarmonyLib;
using Brutal.VulkanApi;
using KSA;
using StarMap.API;

namespace StarMap.Core.Patches
{
    /// <summary>
    /// Patches PlanetRenderer.ProcessExporter to allow mods to perform GPU rendering
    /// during the Vulkan render pass.
    /// </summary>
    [HarmonyPatch(typeof(PlanetRenderer))]
    internal static class PlanetRendererPatcher
    {
        private const string ProcessExporterMethodName = "ProcessExporter";

        [HarmonyPatch(ProcessExporterMethodName)]
        [HarmonyPostfix]
        public static void AfterProcessExporter(
            CommandBuffer commandBuffer, 
            Celestial nearbyCelestial, 
            Viewport viewport, 
            int frameIndex)
        {
            var methods = StarMapCore.Instance?.Loader.ModRegistry.Get<StarMapAfterPlanetExporterRenderAttribute>() ?? [];

            foreach (var (_, @object, method) in methods)
            {
                method.Invoke(@object, [commandBuffer, nearbyCelestial, viewport, frameIndex]);
            }
        }
    }
}
