using HarmonyLib;
using KSA;
using StarMap.API;
using StarMap.API.Interfaces;
using StarMap.Core.ModRepository;
using StarMap.Core.UI.ConfirmRestart;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace StarMap.Core.Patches
{
    internal class StarMapPatches
    {
        private ModRegistry _registry;

        public StarMapPatches(ModRegistry registry, PatchManager patchManager)
        {
            _registry = registry;

            patchManager.RegisterStaticVoidPatch(IPatchHelper.PatchType.Prefix, null, typeof(ModLibrary).GetMethod(nameof(ModLibrary.LoadAll))!, BeforePrepareAll);

            if (DocumentsPathOverride.ShouldApplyOverride())
                patchManager.RegisterStaticPatch<string>(IPatchHelper.PatchType.Prefix, null, AccessTools.PropertyGetter(typeof(Constants), nameof(Constants.DocumentsFolderPath)), DocumentsPathOverride.BeforeDocumentsFolderPath);


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

            return false;
        }
    }
}
