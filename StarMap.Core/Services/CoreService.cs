using StarMap.Core.ModRepository;
using StarMap.Core.Patches;

namespace StarMap.Core.Services
{
    internal interface ICoreService
    {
        void Init();
    }

    internal class CoreService(IStarMapPatches _starmapPatches, ISaveLoadPatches _saveLoadPatches, IModsLoader _modsLoader) : ICoreService
    {
        public void Init()
        {
            _starmapPatches.Init();
            _saveLoadPatches.Init();
            _modsLoader.Init();
        }
    }
}
