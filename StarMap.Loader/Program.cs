using StarMap.Types;
using StarMap.Types.Pipes;
using System.Runtime.Loader;

namespace StarMap
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("StarMap - Running Starmap in solo mode!");
                SoleModeInner();
                return;
            }

            Console.WriteLine("StarMap - Running Starmap in loader mode.");

            var pipeName = args[0];
            Console.WriteLine($"StarMap - Connection to pipe: {pipeName}");

            MainInner(pipeName).GetAwaiter().GetResult();
        }

        static void SoleModeInner()
        {
            var gameConfig = new LoaderConfig();

            if (!gameConfig.TryLoadConfig())
            {
                return;
            }

            AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath("./0Harmony.dll"));

            var gameAssemblyContext = new GameAssemblyLoadContext(gameConfig.GameLocation);
            var dumbFacade = new SoloGameFacade();
            using var gameSurveyer = new GameSurveyer(dumbFacade, gameAssemblyContext, gameConfig.GameLocation);
            if (!gameSurveyer.TryLoadCoreAndGame())
            {
                Console.WriteLine("StarMap - Unable to load mod manager and game in solo mode.");
                return;
            }

            gameSurveyer.RunGame();
        }

        static async Task MainInner(string pipeName)
        {
            AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath("./0Harmony.dll"));

            var pipeClient = new PipeClient(pipeName);
            var facade = new GameFacade(pipeClient);

            var gameLocation = await facade.Connect();
            var gameAssemblyContext = new GameAssemblyLoadContext(Path.GetFullPath(gameLocation));
            var gameSurveyer = new GameSurveyer(facade, gameAssemblyContext, gameLocation);
            if (!gameSurveyer.TryLoadCoreAndGame()) return;

            gameSurveyer.RunGame();

            await facade.DisposeAsync();
        }
    }
}
