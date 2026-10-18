namespace StarMap.Core.Patches
{
    internal static class DocumentsPathOverride
    {
        private const string CliFlag = "-InstancePath";
        private const string EnvVarName = "STARMAP_INSTANCE_PATH";

        public static bool ShouldApplyOverride()
        {
            var overridePath = TryGetOverride();
            return !string.IsNullOrEmpty(overridePath);
        }

        public static bool BeforeDocumentsFolderPath(object[] args, ref string result)
        {
            result = TryGetOverride()!;
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