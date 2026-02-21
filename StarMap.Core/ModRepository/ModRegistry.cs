using KSA;
using StarMap.API;
using StarMap.Core.Config;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace StarMap.Core.ModRepository
{
    internal sealed class ModRegistry : IDisposable
    {
        public Dictionary<string, List<RuntimeMod>> WaitingModsDependencyGraph { get; } = [];
        public HashSet<RuntimeMod> WaitingMods { get; } = [];

        private readonly Dictionary<string, RuntimeMod> _mods = [];
        private readonly Dictionary<Type, List<(StarMapMethodAttribute attribute, object @object, MethodInfo method)>> _modMethods = [];

        public bool ModLoaded(string modId) => _mods.ContainsKey(modId);

        public bool TryGetMod(string modId, [NotNullWhen(true)] out RuntimeMod? modInfo)
        {
            return _mods.TryGetValue(modId, out modInfo);
        }

        public void Add(RuntimeMod modInfo)
        {
            _mods.Add(modInfo.ModId, modInfo);
        }

        public IEnumerable<RuntimeMod> GetMods()
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
