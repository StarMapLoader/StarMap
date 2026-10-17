
using StarMap.API.Interfaces;
using StarMap.Core.Patches;
using System.Reflection;

namespace StarMap.Core.API
{
    internal class PatchHelper : IPatchHelper
    {
        private IPatchManager _patchManager;
        private IModContext _modContext;

        public PatchHelper(IPatchManager patchManager, IModContext mod)
        {
            _patchManager = patchManager;
            _modContext = mod;
        }

        void IPatchHelper.RegisterInstancePatch<T, U>(IPatchHelper.PatchType type, MethodBase method, IPatchHelper.InstanceCallback<T, U> callback)

        {
            _patchManager.RegisterInstancePatch(type, _modContext.Mod, method, callback);
        }

        void IPatchHelper.RegisterInstanceVoidPatch<T>(IPatchHelper.PatchType type, MethodBase method, IPatchHelper.InstanceVoidCallback<T> callback)
        {
            _patchManager.RegisterInstanceVoidPatch(type, _modContext.Mod, method, callback);
        }

        void IPatchHelper.RegisterStaticPatch<U>(IPatchHelper.PatchType type, MethodBase method, IPatchHelper.StaticCallback<U> callback)
        {
            _patchManager.RegisterStaticPatch(type, _modContext.Mod, method, callback);
        }

        void IPatchHelper.RegisterStaticVoidPatch(IPatchHelper.PatchType type, MethodBase method, IPatchHelper.StaticVoidCallback callback)
        {
            _patchManager.RegisterStaticVoidPatch(type, _modContext.Mod, method, callback);
        }
    }
}
