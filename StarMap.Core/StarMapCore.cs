using HarmonyLib;
using StarMap.Core.ModRepository;
using StarMap.Types;
using System.Runtime.Loader;

namespace StarMap.Core
{
    internal class StarMapCore : IStarMapCore
    {
        public static StarMapCore? Instance;

        private readonly Harmony _harmony = new("StarMap.Core");
        private readonly AssemblyLoadContext _coreAssemblyLoadContext;

        private readonly ModLoader _loader;
        public ModLoader Loader => _loader;

        public StarMapCore(AssemblyLoadContext coreAssemblyLoadContext)
        {
            Instance = this;
            _coreAssemblyLoadContext = coreAssemblyLoadContext;
            _loader = new(_coreAssemblyLoadContext);
        }

        public void Init()
        {
            _loader.Init();
            _harmony.PatchAll(typeof(StarMapCore).Assembly);
        }

        public void DeInit()
        {
            _harmony.UnpatchAll();
            _loader.Dispose();
        }
    }
}