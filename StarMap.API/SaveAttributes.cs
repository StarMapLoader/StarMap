using System.IO;
using System.Reflection;

namespace StarMap.API
{
    /// <summary>
    /// Methods marked with this attribute will be called after KSA has written a save.
    /// </summary>
    /// <remarks>
    /// Methods using this attribute must match the following signature:
    ///
    /// <code>
    /// public void MethodName(System.IO.DirectoryInfo saveDirectory);
    /// </code>
    ///
    /// Parameter requirements:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <paramref name="saveDirectory"/> is the folder the save was written to.
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
    public sealed class StarMapAfterSaveAttribute : StarMapMethodAttribute
    {
        public override bool IsValidSignature(MethodInfo method)
        {
            return method.ReturnType == typeof(void) &&
                   method.GetParameters().Length == 1 &&
                   method.GetParameters()[0].ParameterType == typeof(DirectoryInfo);
        }
    }

    /// <summary>
    /// Methods marked with this attribute will be called after KSA has loaded a save.
    /// </summary>
    /// <remarks>
    /// Methods using this attribute must match the following signature:
    ///
    /// <code>
    /// public void MethodName(System.IO.DirectoryInfo saveDirectory);
    /// </code>
    ///
    /// Parameter requirements:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <paramref name="saveDirectory"/> is the folder the save was read from.
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
    public sealed class StarMapAfterLoadAttribute : StarMapMethodAttribute
    {
        public override bool IsValidSignature(MethodInfo method)
        {
            return method.ReturnType == typeof(void) &&
                   method.GetParameters().Length == 1 &&
                   method.GetParameters()[0].ParameterType == typeof(DirectoryInfo);
        }
    }
}
