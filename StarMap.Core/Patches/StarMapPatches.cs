using HarmonyLib;
using KSA;
using StarMap.API.Interfaces;
using StarMap.Core.UI.ConfirmRestart;
using System.Diagnostics;

namespace StarMap.Core.Patches
{
    internal interface IStarMapPatches
    {
        void Init();
    }

    internal class StarMapPatches : IStarMapPatches
    {
        private readonly IPatchManager _patchManager;

        public StarMapPatches(IPatchManager patchManager)
        {
            _patchManager = patchManager;
        }

        public void Init()
        {
            _patchManager.RegisterStaticVoidPatch(IPatchHelper.PatchType.Prefix, null, typeof(ModLibrary).GetMethod(nameof(ModLibrary.LoadAll))!, BeforePrepareAll);

            if (DocumentsPathOverride.ShouldApplyOverride())
                _patchManager.RegisterStaticPatch<string>(IPatchHelper.PatchType.Prefix, null, AccessTools.PropertyGetter(typeof(Constants), nameof(Constants.DocumentsFolderPath)), DocumentsPathOverride.BeforeDocumentsFolderPath);
        }

        private bool BeforePrepareAll(object[] _)
        {
            static bool wereNewModsEnabled()
            {
                foreach (var mod in ModLibrary.Manifest.Mods)
                {
                    if (mod.New && mod.Enabled)
                        return true;
                }
                return false;
            }

            if (ModLibrary.HasNewMods() && wereNewModsEnabled())
            {
                var confirmRestart = new ConfirmRestart();
                while (confirmRestart.Show)
                {
                    confirmRestart.OnFrame();
                }

                if (confirmRestart.Restart)
                {
                    Console.WriteLine("StarMap - RESTARTING");
                    Console.WriteLine("======================================");

                    var mainModule = Process.GetCurrentProcess().MainModule;
                    if (mainModule is null || Path.GetDirectoryName(mainModule.FileName) is not string starMapInstallLocation)
                    {
                        Console.WriteLine("Unable to get StarMap install location for restart.");
                    }
                    else
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = mainModule.FileName,
                            Arguments = "--restarted",
                            WorkingDirectory = starMapInstallLocation,
                        };

                        Process.Start(startInfo);
                    }

                    Environment.Exit(0);
                }
            }

            return true;
        }
    }
}
