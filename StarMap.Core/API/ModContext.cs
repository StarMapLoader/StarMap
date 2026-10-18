using StarMap.Core.ModRepository;

namespace StarMap.Core.API
{
    internal interface IModContext
    {
        RuntimeMod Mod { get; set; }
    }

    internal sealed class ModContext : IModContext
    {
        public RuntimeMod Mod { get; set; } = null!;
    }
}
