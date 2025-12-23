using HarmonyLib;
using Brutal.VulkanApi;
using KSA;
using StarMap.API;
using System;

namespace StarMap.Core.Patches
{
    /// <summary>
    /// Patches PlanetRenderer.ProcessExporter to allow mods to perform GPU rendering
    /// during the Vulkan render pass.
    /// </summary>
    [HarmonyPatch(typeof(PlanetRenderer), "ProcessExporter")]
    internal static class PlanetRendererPatcher
    {
        [HarmonyPostfix]
        public static void Postfix(
            CommandBuffer commandBuffer, 
            Celestial nearbyCelestial, 
            Viewport viewport, 
            int frameIndex)
        {
            var methods = StarMapCore.Instance?.LoadedMods.Mods.Get<StarMapOnRenderAttribute>() ?? [];

            foreach (var (_, @object, method) in methods)
            {
                method.Invoke(@object, [commandBuffer, nearbyCelestial, viewport, frameIndex]);
            }
        }
    }
}
