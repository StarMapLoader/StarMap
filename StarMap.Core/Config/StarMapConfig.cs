using System;
using System.Collections.Generic;
using System.Text;

namespace StarMap.Core.Config
{
    internal class RootConfig 
    { 
        public required StarMapConfig StarMap { get; set; }
    }


    internal class StarMapConfig
    {
        public required string EntryAssembly { get; set; }
        public List<string> ExportedAssemblies { get; set; } = [];
        public List<StarMapModDependency> ModDependencies { get; set; } = [];
    }

    internal class StarMapModDependency
    {
        public required string ModId { get; set; }
        public bool Optional { get; set; } = false;
        public List<string> ImportedAssemblies { get; set; } = [];
    }
}
