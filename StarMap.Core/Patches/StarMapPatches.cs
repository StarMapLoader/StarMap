using HarmonyLib;
using KSA;
using StarMap.API.Services;
using StarMap.Core.Services;
using StarMap.Core.UI.ConfirmRestart;
using System.Diagnostics;

namespace StarMap.Core.Patches
{
    internal interface IStarMapPatches : IDisposable
    {
        void Init();
    }

    internal class StarMapPatches : IStarMapPatches
    {
        private readonly IPatchManager _patchManager;
        private readonly ILogger _logger;
        private readonly HashSet<IDisposable> _patches = [];

        public StarMapPatches(IPatchManager patchManager, ILogger logger)
        {
            _patchManager = patchManager;
            _logger = logger;
        }

        public void Dispose()
        {
            foreach (var patch in _patches)
            {
                patch.Dispose();
            }
            _patches.Clear();
        }

        public void Init()
        {
            _patches.Add(_patchManager.RegisterStaticVoidPatch(IPatchService.PatchType.Prefix, null, typeof(ModLibrary).GetMethod(nameof(ModLibrary.PrepareAll))!, BeforePrepareAll));

            if (DocumentsPathOverride.ShouldApplyOverride())
                _patches.Add(_patchManager.RegisterStaticPatch<string>(IPatchService.PatchType.Prefix, null, AccessTools.PropertyGetter(typeof(Constants), nameof(Constants.DocumentsFolderPath)), DocumentsPathOverride.BeforeDocumentsFolderPath));
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
                    _logger.Information("StarMap - RESTARTING");
                    _logger.Information("======================================");

                    var mainModule = Process.GetCurrentProcess().MainModule;
                    if (mainModule is null || Path.GetDirectoryName(mainModule.FileName) is not string starMapInstallLocation)
                    {
                        _logger.Warning("Unable to get StarMap install location for restart.");
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
