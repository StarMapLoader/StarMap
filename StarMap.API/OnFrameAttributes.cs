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
    ///       <paramref name="currentPlayerTime"/>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <paramref name="dtPlayer"/>
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
}
