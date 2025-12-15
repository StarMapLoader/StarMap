using StarMap.Types.Mods;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace StarMap
{
    internal class GameSurveyer : IDisposable
    {
        private readonly IGameFacade _facade;
        private readonly AssemblyLoadContext _gameAssemblyContext;
        private readonly string _gameLocation;

        private Assembly? _game;
        private IStarMapCore? _core;

        public GameSurveyer(IGameFacade facade, AssemblyLoadContext alc, string location)
        {
            _facade = facade;
            _gameAssemblyContext = alc;
            _gameLocation = location;
        }

        public bool TryLoadCoreAndGame()
        {
            var modManagerAssembly = _gameAssemblyContext.LoadFromAssemblyPath(Path.GetFullPath("./StarMap.Core.dll"));

            var starMapCore = modManagerAssembly.GetTypes().FirstOrDefault((type) => typeof(IStarMapCore).IsAssignableFrom(type) && !type.IsInterface);
            if (starMapCore is null) return false;
            var createdCore = Activator.CreateInstance(starMapCore, [_gameAssemblyContext]);
            if (createdCore is not IStarMapCore core) return false;

            _game = _gameAssemblyContext.LoadFromAssemblyPath(_gameLocation);

            var gameDirectory = Path.GetDirectoryName(_gameLocation);
            if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            {
                Console.WriteLine("StarMap - Game directory not found");
                return false;
            }

            Directory.SetCurrentDirectory(gameDirectory);

            _core = core;
            core.Init();
            return true;
        }

        public void RunGame()
        {
            Debug.Assert(_game is not null, "Load needs to be called before running game");

            string[] args = [];
            
            _game.EntryPoint!.Invoke(null, [args]);
        }

        public void Dispose()
        {
            _core?.DeInit();
        }
    }
}
