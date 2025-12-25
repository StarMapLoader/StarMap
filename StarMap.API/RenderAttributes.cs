using Brutal.VulkanApi;
using KSA;
using System.Reflection;

namespace StarMap.API
{
    /// <summary>
    /// Methods marked with this attribute will be called after KSA Program.OnFrame is called.
    /// </summary>
    /// <remarks>
    /// Methods using this attribute must match the following signature:
    ///
    /// <code>
    /// public void MethodName(double currentPlayerTime, double dtPlayer);
    /// </code>
    ///
    /// Parameter requirements:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <c>currentPlayerTime</c> – The current simulation time of the player.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>dtPlayer</c> – Delta time since the last player update.
    ///     </description>
    ///   </item>
    /// </list>
    /// 
    /// Requirements:
    /// <list type="bullet">
    ///   <item><description>Return type must be <see cref="void"/>.</description></item>
    ///   <item><description>Method must be an instance method (non-static).</description></item>
    /// </list>
    /// </remarks>
    public sealed class StarMapAfterOnFrameAttribute : StarMapMethodAttribute
    {
        public override bool IsValidSignature(MethodInfo method)
        {
            return method.ReturnType == typeof(void) &&
                   method.GetParameters().Length == 2 &&
                   method.GetParameters()[0].ParameterType == typeof(double) &&
                   method.GetParameters()[1].ParameterType == typeof(double);

        }
    }

    /// <summary>
    /// Methods marked with this attribute will be called during the Vulkan render pass,
    /// after KSA's ground track map rendering but before ocean/atmosphere rendering.
    /// This is the appropriate place for mods to perform GPU rendering operations
    /// that require access to the Vulkan command buffer.
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
    ///       <c>commandBuffer</c> – The Vulkan command buffer used to record render commands.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>celestial</c> – The nearby <see cref="Celestial"/> body being rendered.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>viewport</c> – The current render <see cref="Viewport"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>frameIndex</c> – The current frame index, used for per-frame resource management.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// Requirements:
    /// <list type="bullet">
    ///   <item>
    ///     <description>Return type must be <see cref="void"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>Method must be an instance method (non-static).</description>
    ///   </item>
    /// </list>
    ///
    /// Note: This hook is only called when near a billboarded celestial body (planet or moon).
    /// </remarks>
    public sealed class StarMapAfterPlanetExporterRenderAttribute : StarMapMethodAttribute
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

    /// <summary>
    /// Methods marked with this attribute will be called before KSA starts creating its ImGui interface.
    /// </summary>
    /// <remarks>
    /// Methods using this attribute must match the following signature:
    ///
    /// <code>
    /// public void MethodName(double dt);
    /// </code>
    ///
    /// Parameter requirements:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <c>dt</c> – Delta time since the previous frame.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// Requirements:
    /// <list type="bullet">
    ///   <item>
    ///     <description>Return type must be <see cref="void"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>Method must be an instance method (non-static).</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public sealed class StarMapBeforeGuiAttribute : StarMapMethodAttribute
    {
        public override bool IsValidSignature(MethodInfo method)
        {
            return method.ReturnType == typeof(void) &&
                   method.GetParameters().Length == 1 &&
                   method.GetParameters()[0].ParameterType == typeof(double);

        }
    }

    /// <summary>
    /// Methods marked with this attribute will be called when KSA has finished creating its ImGui interface.
    /// </summary>
    /// <remarks>
    /// Methods using this attribute must match the following signature:
    ///
    /// <code>
    /// public void MethodName(double dt);
    /// </code>
    ///
    /// Parameter requirements:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <c>dt</c> – Delta time since the last ImGui update.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// Requirements:
    /// <list type="bullet">
    ///   <item>
    ///     <description>Return type must be <see cref="void"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>Method must be an instance method (non-static).</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public sealed class StarMapAfterGuiAttribute : StarMapMethodAttribute
    {
        public override bool IsValidSignature(MethodInfo method)
        {
            return method.ReturnType == typeof(void) &&
                   method.GetParameters().Length == 1 &&
                   method.GetParameters()[0].ParameterType == typeof(double);

        }
    }
}
