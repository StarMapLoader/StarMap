using KSA;
using StarMap.API;
using StarMap.Core.Config;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace StarMap.Core.ModRepository
{
    internal sealed class ModInformation
    {
        public required string ModId { get; init; }
        public required ModAssemblyLoadContext ModAssemblyLoadContext { get; init; }
        public required Type ModType { get; init; }
        public required StarMapConfig Config { get; init; }

        public bool Initialized { get; set; } = false;
        public object? ModInstance { get; set; } = null;

        public HashSet<string> ExportedAssemblies { get; set; } = [];
        public Dictionary<ModInformation, HashSet<string>> Dependencies { get; set; } = [];
        public Dictionary<string, StarMapModDependency> NotLoadedModDependencies { get; set; } = [];


        public MethodInfo? BeforeMainAction { get; set; } = null;
        public MethodInfo? PrepareSystemsAction { get; set; } = null;
    }

    internal sealed class ModRegistry : IDisposable
    {
        private readonly Dictionary<string, ModInformation> _mods = [];
        private readonly Dictionary<Type, List<(StarMapMethodAttribute attribute, object @object, MethodInfo method)>> _modMethods = [];

        public bool ModLoaded(string modId) => _mods.ContainsKey(modId);

        public bool TryGetMod(string modId, [NotNullWhen(true)] out ModInformation? modInfo)
        {
            return _mods.TryGetValue(modId, out modInfo);
        }

        public void Add(ModInformation modInfo)
        {
            _mods.Add(modInfo.ModId, modInfo);
        }

        public IEnumerable<ModInformation> GetMods()
        {
            return _mods.Values;
        }


        public void AddModMethod(string modId, StarMapMethodAttribute methodAttribute, object @object, MethodInfo method)
        {
            if (!_mods.TryGetValue(modId, out var modInfo)) return;

            var attributeType = methodAttribute.GetType();

            if (!_modMethods.TryGetValue(attributeType, out var list))
            {
                list = [];
                _modMethods[attributeType] = list;
            }

            if (methodAttribute.GetType() == typeof(StarMapBeforeMainAttribute))
                modInfo.BeforeMainAction = method;

            if (methodAttribute.GetType() == typeof(StarMapImmediateLoadAttribute))
                modInfo.PrepareSystemsAction = method;

            list.Add((methodAttribute, @object, method));
        }

        public IReadOnlyList<(StarMapMethodAttribute attribute, object @object, MethodInfo method)> Get<TAttribute>()
            where TAttribute : Attribute
        {
            if (_modMethods.TryGetValue(typeof(TAttribute), out var list))
            {
                return list.Cast<(StarMapMethodAttribute attribute, object @object, MethodInfo method)>().ToList();
            }

            return Array.Empty<(StarMapMethodAttribute attribute, object @object, MethodInfo method)>();
        }

        public IReadOnlyList<(StarMapMethodAttribute attribute, object @object, MethodInfo method)> Get(Type iface)
        {
            return _modMethods.TryGetValue(iface, out var list)
                ? list
                : Array.Empty<(StarMapMethodAttribute attribute, object @object, MethodInfo method)>();
        }

        public void Dispose()
        {
            _modMethods.Clear();
        }
    }
}
