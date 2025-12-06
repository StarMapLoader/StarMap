using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace StarMap
{
    internal class GameAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _gameDependencyResolver;
        private readonly AssemblyDependencyResolver _starMapDepdencyResolver;

        public GameAssemblyLoadContext(string gamePath)
            : base()
        {
            _gameDependencyResolver = new AssemblyDependencyResolver(gamePath);

            _starMapDepdencyResolver = new AssemblyDependencyResolver(
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

            path = _starMapDepdencyResolver.ResolveAssemblyToPath(assemblyName);

            return path != null ? LoadFromAssemblyPath(path) : null;
        }

    }
}