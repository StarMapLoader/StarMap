using System.Runtime.Loader;

namespace StarMap
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // A game restart inherits the game's working directory.
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            var gameConfig = new LoaderConfig();

            if (!gameConfig.TryLoadConfig())
            {
                return;
            }

            AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath("./0Harmony.dll"));

            var gameAssemblyContext = new CoreAssemblyLoadContext(gameConfig.GameLocation);
            using var gameSurveyer = new GameSurveyer(gameAssemblyContext, gameConfig.GameLocation, [.. args, .. gameConfig.GameArguments]);
            if (!gameSurveyer.TryLoadCoreAndGame())
            {
                Console.WriteLine("StarMap - Unable to load mod manager and game.");
                return;
            }

            gameSurveyer.RunGame();
        }
    }
}
