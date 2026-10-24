using StarMap.API.Registries;
using StarMap.Core.ModRepository;
using StarMap.Core.Services;

namespace StarMap.Core.API
{
    internal class ModServiceRegistry : IModServiceRegistry
    {
        private readonly RuntimeMod _mod;
        private readonly IModServices _modServices;

        public ModServiceRegistry(IModContext modContext, IModServices modServices)
        {
            _mod = modContext.Mod;
            _modServices = modServices;
        }

        public T Get<T>()
        {
            return _modServices.Get<T>();
        }

        public void RegisterService<T>(T implementation)
        {
            _modServices.Register(_mod, implementation);
        }
    }
}
