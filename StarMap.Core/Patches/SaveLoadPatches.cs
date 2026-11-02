using HarmonyLib;
using KSA;
using StarMap.API;
using StarMap.API.Services;
using StarMap.Core.ModRepository;
using System.Reflection;

namespace StarMap.Core.Patches
{
    internal interface ISaveLoadPatches
    {
        void Init();
    }

    internal class SaveLoadPatches : ISaveLoadPatches
    {
        private readonly IPatchManager _patchManager;
        private readonly IModsLoader _modsLoader;

        // UncompressedSave.Load returns early when the save cannot be read, so its postfix alone
        // would report loads that never happened. OnGameLoaded runs only once the world is rebuilt.
        private bool _worldRebuilt;

        public SaveLoadPatches(IPatchManager patchManager, IModsLoader modsLoader)
        {
            _patchManager = patchManager;
            _modsLoader = modsLoader;
        }

        public void Init()
        {
            // Every save, overwrite and save-as goes through Make.
            var make = AccessTools.Method(typeof(UncompressedSave), nameof(UncompressedSave.Make), [typeof(string)]);
            var load = AccessTools.Method(typeof(UncompressedSave), nameof(UncompressedSave.Load), Type.EmptyTypes);
            var gameLoaded = AccessTools.Method(typeof(Program), nameof(Program.OnGameLoaded), Type.EmptyTypes);

            if (make is null)
            {
                Console.WriteLine("StarMap - UncompressedSave.Make(string) not found; StarMapAfterSave will not be called");
            }
            else
            {
                _patchManager.RegisterStaticPatch<GameSave?>(IPatchService.PatchType.Postfix, null, make, AfterMake);
            }

            if (load is null || gameLoaded is null)
            {
                Console.WriteLine("StarMap - UncompressedSave.Load() or Program.OnGameLoaded() not found; StarMapAfterLoad will not be called");
                return;
            }

            _patchManager.RegisterInstanceVoidPatch<UncompressedSave>(IPatchService.PatchType.Prefix, null, load, BeforeLoad);
            _patchManager.RegisterInstanceVoidPatch<UncompressedSave>(IPatchService.PatchType.Postfix, null, load, AfterLoad);
            _patchManager.RegisterStaticVoidPatch(IPatchService.PatchType.Postfix, null, gameLoaded, AfterGameLoaded);
        }

        private bool AfterMake(object[] args, ref GameSave? result)
        {
            if (result is UncompressedSave save)
                Dispatch<StarMapAfterSaveAttribute>(save.Directory);

            return true;
        }

        private bool BeforeLoad(object[] args, UncompressedSave save)
        {
            _worldRebuilt = false;
            return true;
        }

        private bool AfterGameLoaded(object[] args)
        {
            _worldRebuilt = true;
            return true;
        }

        private bool AfterLoad(object[] args, UncompressedSave save)
        {
            if (!_worldRebuilt)
                return true;

            _worldRebuilt = false;
            Dispatch<StarMapAfterLoadAttribute>(save.Directory);
            return true;
        }

        // These run inside KSA's save and load, so one mod's exception must not reach the game
        // or stop the mods after it.
        private void Dispatch<TAttribute>(DirectoryInfo directory) where TAttribute : StarMapMethodAttribute
        {
            foreach (var (_, modObject, method) in _modsLoader.ModRegistry.Get<TAttribute>())
            {
                try
                {
                    method.Invoke(modObject, [directory]);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"StarMap - {typeof(TAttribute).Name} in {method.DeclaringType?.FullName} threw: {(e as TargetInvocationException)?.InnerException ?? e}");
                }
            }
        }
    }
}
