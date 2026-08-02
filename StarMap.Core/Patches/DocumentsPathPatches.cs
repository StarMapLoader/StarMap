using HarmonyLib;
using KSA;

namespace StarMap.Core.Patches
{
    internal static class DocumentsPathPatches
    {
        private const string CliFlag = "-InstancePath";
        private const string EnvVarName = "STARMAP_INSTANCE_PATH";
        private const string HarmonyId = "com.starmap.core.documentspathoverride";

        public static void Apply()
        {
            // Check if an override path is provided via command-line argument or environment variable
            var overridePath = TryGetOverride();
            if (string.IsNullOrEmpty(overridePath))
                return;

            // Apply the Harmony patch to override the DocumentsFolderPath property
            var harmony = new Harmony(HarmonyId);
            var original = AccessTools.PropertyGetter(typeof(Constants), nameof(Constants.DocumentsFolderPath));
            var prefix = new HarmonyMethod(typeof(DocumentsPathPatches), nameof(Prefix));
            harmony.Patch(original, prefix: prefix);
            Console.WriteLine($"StarMap - Using Instance Path: {overridePath}");
        }

        private static bool Prefix(ref string __result)
        {
            __result = TryGetOverride()!;
            return false;
        }

        private static string? TryGetOverride()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], CliFlag, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return Environment.GetEnvironmentVariable(EnvVarName);
        }
    }
}