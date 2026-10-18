using StarMap.API.Services;
using StarMap.Core.Patches;
using System.Reflection;

namespace StarMap.Core.API
{
    internal class PatchService : IPatchService
    {
        private IPatchManager _patchManager;
        private IModContext _modContext;

        public PatchService(IPatchManager patchManager, IModContext mod)
        {
            _patchManager = patchManager;
            _modContext = mod;
        }

        void IPatchService.RegisterInstancePatch<T, U>(IPatchService.PatchType type, MethodBase method, IPatchService.InstanceCallback<T, U> callback)

        {
            _patchManager.RegisterInstancePatch(type, _modContext.Mod, method, callback);
        }

        void IPatchService.RegisterInstanceVoidPatch<T>(IPatchService.PatchType type, MethodBase method, IPatchService.InstanceVoidCallback<T> callback)
        {
            _patchManager.RegisterInstanceVoidPatch(type, _modContext.Mod, method, callback);
        }

        void IPatchService.RegisterStaticPatch<U>(IPatchService.PatchType type, MethodBase method, IPatchService.StaticCallback<U> callback)
        {
            _patchManager.RegisterStaticPatch(type, _modContext.Mod, method, callback);
        }

        void IPatchService.RegisterStaticVoidPatch(IPatchService.PatchType type, MethodBase method, IPatchService.StaticVoidCallback callback)
        {
            _patchManager.RegisterStaticVoidPatch(type, _modContext.Mod, method, callback);
        }
    }
}
