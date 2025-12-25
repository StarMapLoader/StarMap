using KSA;
using System.Reflection;

namespace StarMap.API
{
    /// <summary>
    /// Marks the main class for a StarMap mod.
    /// Only attributes on methods within classes marked with this attribute will be considered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class StarMapModAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public abstract class StarMapMethodAttribute : Attribute
    {
        public abstract bool IsValidSignature(MethodInfo info);
    }

    /// <summary>
    /// Methods marked with this attribute will be called before KSA is started.
    /// </summary>
    /// <remarks>
    /// Methods using this attribute must match the following signature:
    ///
    /// <code>
    /// public void MethodName();
    /// </code>
    ///
    /// Specifically:
    /// <list type="bullet">
    ///   <item><description>No parameters are allowed.</description></item>
    ///   <item><description>Return type must be <see cref="void"/>.</description></item>
    ///   <item><description>Method must be an instance method (non-static).</description></item>
    /// </list>
    /// </remarks>
    public class StarMapBeforeMainAttribute : StarMapMethodAttribute
    {
        public override bool IsValidSignature(MethodInfo method)
        {
            return method.ReturnType == typeof(void) &&
                   method.GetParameters().Length == 0;
        }
    }

    /// <summary>
    /// Methods marked with this attribute will be called immediately when the mod is loaded by KSA.
    /// </summary>
    /// <remarks>
    /// Methods using this attribute must match the following signature:
    ///
    /// <code>
    /// public void MethodName(KSA.Mod definingMod);
    /// </code>
    ///
    /// The parameter <c>definingMod</c> is the <see cref="Mod"/> instance that is being loaded.
    /// 
    /// Requirements:
    /// <list type="bullet">
    ///   <item>
    ///     <description>Return type must be <see cref="void"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>Method must be an instance method (non-static).</description>
    ///   </item>
    ///   <item>
    ///     <description>Method must have exactly one parameter of type <see cref="Mod"/>.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public class StarMapImmediateLoadAttribute : StarMapMethodAttribute
    {
        public override bool IsValidSignature(MethodInfo method)
        {
            return method.ReturnType == typeof(void) &&
                   method.GetParameters().Length == 1 &&
                   method.GetParameters()[0].ParameterType == typeof(Mod);
        }
    }

    /// <summary>
    /// Methods marked with this attribute will be called when all mods are loaded.
    /// </summary>
    /// <remarks>
    /// Methods using this attribute must follow this signature:
    ///
    /// <code>
    /// public void MethodName();
    /// </code>
    /// 
    /// Specifically:
    /// <list type="bullet">
    ///   <item><description>No parameters are allowed.</description></item>
    ///   <item><description>Return type must be <see cref="void"/>.</description></item>
    ///   <item><description>Method must be an instance method (non-static).</description></item>
    /// </list>
    /// </remarks>
    public class StarMapAllModsLoadedAttribute : StarMapMethodAttribute
    {
        public override bool IsValidSignature(MethodInfo method)
        {
            return method.ReturnType == typeof(void) &&
                   method.GetParameters().Length == 0;
        }
    }

    /// <summary>
    /// Methods marked with this attribute will be called when KSA is unloaded
    /// </summary>
    /// <remarks>
    /// Methods using this attribute must follow this signature:
    ///
    /// <code>
    /// public void MethodName();
    /// </code>
    /// 
    /// Specifically:
    /// <list type="bullet">
    ///   <item><description>No parameters are allowed.</description></item>
    ///   <item><description>Return type must be <see cref="void"/>.</description></item>
    ///   <item><description>Method must be an instance method (non-static).</description></item>
    /// </list>
    /// </remarks>
    public class StarMapUnloadAttribute : StarMapMethodAttribute
    {
        public override bool IsValidSignature(MethodInfo method)
        {
            return method.ReturnType == typeof(void) &&
                   method.GetParameters().Length == 0;
        }
    }
}
