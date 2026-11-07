using StarMap.API.Services;
using StarMap.Core.ModRepository;
using StarMap.Core.Patches;
using System.Reflection;

namespace StarMap.Core.API
{
    internal class PatchService : IPatchService
    {
        private readonly IPatchManager _patchManager;
        private readonly IModContext _modContext;

        public PatchService(IPatchManager patchManager, IModContext mod)
        {
            _patchManager = patchManager;
            _modContext = mod;
        }

        public IDisposable RegisterInstancePatch<T, U>(IPatchService.PatchType type, MethodBase method, IPatchService.InstanceCallback<T, U> callback)

        {
            var patch = _patchManager.RegisterInstancePatch(type, _modContext.Mod, method, callback);
            var patchHandle = new PatchDisposeHandle()
            {
                Patch = patch,
                Mod = _modContext.Mod
            };
            _modContext.Mod.Patches.Add(patchHandle);
            return patchHandle;
        }

        public IDisposable RegisterInstanceVoidPatch<T>(IPatchService.PatchType type, MethodBase method, IPatchService.InstanceVoidCallback<T> callback)
        {
            var patch = _patchManager.RegisterInstanceVoidPatch(type, _modContext.Mod, method, callback);
            var patchHandle = new PatchDisposeHandle()
            {
                Patch = patch,
                Mod = _modContext.Mod
            };
            _modContext.Mod.Patches.Add(patchHandle);
            return patchHandle;
        }

        public IDisposable RegisterStaticPatch<U>(IPatchService.PatchType type, MethodBase method, IPatchService.StaticCallback<U> callback)
        {
            var patch = _patchManager.RegisterStaticPatch(type, _modContext.Mod, method, callback);
            var patchHandle = new PatchDisposeHandle()
            {
                Patch = patch,
                Mod = _modContext.Mod
            };
            _modContext.Mod.Patches.Add(patchHandle);
            return patchHandle;
        }

        public IDisposable RegisterStaticVoidPatch(IPatchService.PatchType type, MethodBase method, IPatchService.StaticVoidCallback callback)
        {
            var patch = _patchManager.RegisterStaticVoidPatch(type, _modContext.Mod, method, callback);
            var patchHandle = new PatchDisposeHandle()
            {
                Patch = patch,
                Mod = _modContext.Mod
            };
            _modContext.Mod.Patches.Add(patchHandle);
            return patchHandle;
        }

        private class PatchDisposeHandle : IDisposable
        {
            public required RuntimeMod Mod { get; init; }
            public required IDisposable Patch { get; init; }

            public void Dispose()
            {
                Patch.Dispose();
                Mod.Patches.Remove(this);
            }
        }
    }
}
