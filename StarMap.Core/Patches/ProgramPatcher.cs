using HarmonyLib;
using KSA;
using StarMap.API;

namespace StarMap.Core.Patches
{
    [HarmonyPatch(typeof(Program))]
    internal static class ProgramPatcher
    {
        private const string OnDrawUiMethodName = "OnDrawUi";

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
    }
}
