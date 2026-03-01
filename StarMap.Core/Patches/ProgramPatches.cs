using HarmonyLib;
using KSA;
using StarMap.API;

namespace StarMap.Core.Patches
{
    [HarmonyPatch(typeof(Program))]
    internal static class ProgramPatches
    {
        private const string OnDrawUiMethodName = "OnDrawUi";
        private const string OnFrameMethodName = "OnFrame";

        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPostfix]
        public static bool AfterProgramConstructor(IReadOnlyList<string> inArgs)
        {
            if (ConfirmModsPatches.HasEnabledMods)
            {
                //AdviceRestartPopup.Create();
            }
            return true;
        }

        [HarmonyPatch(OnDrawUiMethodName)]
        [HarmonyPrefix]
        public static void BeforeOnDrawUi(double dt)
        {
            var methods = StarMapCore.Instance?.Loader.ModRegistry.Get<StarMapBeforeGuiAttribute>() ?? [];

            foreach (var (_, @object, method) in methods)
            {
                method.Invoke(@object, [dt]);
            }
        }

        [HarmonyPatch(OnDrawUiMethodName)]
        [HarmonyPostfix]
        public static void AfterOnDrawUi(double dt)
        {
            var methods = StarMapCore.Instance?.Loader.ModRegistry.Get<StarMapAfterGuiAttribute>() ?? [];

            foreach (var (_, @object, method) in methods)
            {
                method.Invoke(@object, [dt]);
            }
        }

        [HarmonyPatch(OnFrameMethodName)]
        [HarmonyPostfix]
        public static void AfterOnFrame(double currentPlayerTime, double dtPlayer)
        {
            var methods = StarMapCore.Instance?.Loader.ModRegistry.Get<StarMapAfterOnFrameAttribute>() ?? [];

            foreach (var (_, @object, method) in methods)
            {
                method.Invoke(@object, new object[] { currentPlayerTime, dtPlayer });
            }
        }
    }
}
