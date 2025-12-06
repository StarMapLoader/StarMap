using KSA;
using System.Reflection;
using System.Runtime.Loader;

namespace StarMap.Core.ModRepository
{
    internal class ModAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyLoadContext _coreAssemblyLoadContext;
        private readonly AssemblyDependencyResolver _modDependencyResolver;

        public ModAssemblyLoadContext(string modId, string modDirectory, AssemblyLoadContext coreAssemblyContext)
            : base()
        {
            _coreAssemblyLoadContext = coreAssemblyContext;

            _modDependencyResolver = new AssemblyDependencyResolver(
                Path.GetFullPath(Path.Combine(modDirectory, modId + ".dll"))
            );
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var existingInDefault = AssemblyLoadContext.Default.Assemblies
                .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
            if (existingInDefault != null)
                return existingInDefault;

            var existingInGameContext = _coreAssemblyLoadContext?.Assemblies
                .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
            if (existingInGameContext != null)
                return existingInGameContext;

            if (_coreAssemblyLoadContext != null)
            {
                try
                {
                    var asm = _coreAssemblyLoadContext.LoadFromAssemblyName(assemblyName);
                    if (asm != null)
                        return asm;
                }
                catch (FileNotFoundException)
                {
                }
            }

            var foundPath = _modDependencyResolver.ResolveAssemblyToPath(assemblyName);
            if (foundPath is null)
                return null;

            return LoadFromAssemblyPath(Path.GetFullPath(foundPath));
        }
    }
}
