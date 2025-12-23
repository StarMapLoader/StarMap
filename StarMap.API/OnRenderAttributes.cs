using System.Reflection;
using Brutal.VulkanApi;
using KSA;

namespace StarMap.API
{
    /// <summary>
    /// Methods marked with this attribute will be called during the Vulkan render pass,
    /// after KSA's ground track map rendering but before ocean/atmosphere rendering.
    /// This is the appropriate place for mods to perform GPU rendering operations
    /// that require access to the CommandBuffer.
    /// </summary>
    /// <remarks>
    /// Methods using this attribute must match the following signature:
    ///
    /// <code>
    /// public void MethodName(CommandBuffer commandBuffer, Celestial celestial, Viewport viewport, int frameIndex);
    /// </code>
    ///
    /// Parameter requirements:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <paramref name="commandBuffer"/> - The Vulkan command buffer for recording render commands.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <paramref name="celestial"/> - The nearby celestial body being rendered.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <paramref name="viewport"/> - The current render viewport.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <paramref name="frameIndex"/> - The current frame index for resource management.
    ///     </description>
    ///   </item>
    /// </list>
    /// 
    /// Requirements:
    /// <list type="bullet">
    ///   <item><description>Return type must be <see cref="void"/>.</description></item>
    ///   <item><description>Method must be an instance method (non-static).</description></item>
    /// </list>
    /// 
    /// Note: This hook is only called when near a billboarded celestial body (planet/moon).
    /// </remarks>
    public sealed class StarMapOnRenderAttribute : StarMapMethodAttribute
    {
        public override bool IsValidSignature(MethodInfo method)
        {
            var parameters = method.GetParameters();
            return method.ReturnType == typeof(void) &&
                   parameters.Length == 4 &&
                   parameters[0].ParameterType == typeof(CommandBuffer) &&
                   parameters[1].ParameterType == typeof(Celestial) &&
                   parameters[2].ParameterType == typeof(Viewport) &&
                   parameters[3].ParameterType == typeof(int);
        }
    }
}
