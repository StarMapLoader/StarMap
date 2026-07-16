using System.Reflection;
using System.Runtime.Loader;

namespace StarMap
{
    internal class CoreAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _gameDependencyResolver;
        private readonly AssemblyDependencyResolver _starMapDependencyResolver;

        public CoreAssemblyLoadContext(string gamePath)
            : base()
        {
            _gameDependencyResolver = new AssemblyDependencyResolver(gamePath);

            _starMapDependencyResolver = new AssemblyDependencyResolver(
                Path.GetFullPath("./StarMap.Core.dll")
            );
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var existingInDefault = Default.Assemblies
                .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
            if (existingInDefault != null)
                return existingInDefault;

            var path = _gameDependencyResolver.ResolveAssemblyToPath(assemblyName);

            if (path is not null)
                return LoadFromAssemblyPath(path);

            path = _starMapDependencyResolver.ResolveAssemblyToPath(assemblyName);

            return path != null ? LoadFromAssemblyPath(path) : null;
        }

    }
}