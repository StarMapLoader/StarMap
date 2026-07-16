using Brutal.Collections;
using HarmonyLib;
using KSA;
using StarMap.API;
using StarMap.Core.ModRepository;
using StarMap.Core.UI;
using StarMap.Core.UI.ConfirmRestart;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;

namespace StarMap.Core.Patches
{
    [HarmonyPatch(typeof(ModLibrary))]
    internal class ModLibraryPatches
    {
        [HarmonyPatch(nameof(ModLibrary.LoadAll))]
        [HarmonyPostfix]
        public static void AfterLoad()
        {
            var modRegistry = StarMapCore.Instance?.Loader.ModRegistry;
            if (modRegistry is not ModRegistry registry) return;

            foreach (var (_, @object, method) in registry.Get<StarMapAllModsLoadedAttribute>())
            {
                method.Invoke(@object, []);
            }
        }

        [HarmonyPatch(nameof(ModLibrary.PrepareAll))]
        [HarmonyPrefix]
        public static void BeforePrepareAll()
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
                while(confirmRestart.Show)
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
                    else {
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
        }
    }
}
