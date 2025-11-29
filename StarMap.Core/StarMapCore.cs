using HarmonyLib;
using StarMap.Core.ModRepository;
using StarMap.Types.Mods;
using System.Runtime.Loader;

namespace StarMap.Core
{
    internal class StarMapCore : IStarMapCore
    {
        public static StarMapCore? Instance;

        private readonly Harmony _harmony = new("StarMap.Core");
        private readonly AssemblyLoadContext _coreAssemblyLoadContext;

        private readonly ModLoader _loadedMods;
        public ModLoader LoadedMods => _loadedMods;

        public StarMapCore(AssemblyLoadContext coreAssemblyLoadContext)
        {
            Instance = this;
            _coreAssemblyLoadContext = coreAssemblyLoadContext;
            _loadedMods = new(_coreAssemblyLoadContext);
        }

        public void Init()
        {
            _loadedMods.Init();
            _harmony.PatchAll(typeof(StarMapCore).Assembly);
        }

        public void DeInit()
        {
            _harmony.UnpatchAll();
            _loadedMods.Dispose();
        }
    }
}

/*public async Task RetrieveManagedMods()
{
    var message = new IPCGetCurrentManagedModsRequest();

    var response = await _gameFacade.RequestData(message);

    if (!response.Is(IPCGetCurrentManagedModsResponse.Descriptor)) return;

    _managedMods.SetResult(response.Unpack<IPCGetCurrentManagedModsResponse>());
}

public IPCGetCurrentManagedModsResponse GetManagedMods()
{
    return _managedMods.Task.GetAwaiter().GetResult();
}

public async Task<IPCMod[]> GetAvailableModsAsync()
{
    var message = new IPCGetModsRequest();

    var response = await _gameFacade.RequestData(message);

    if (!response.Is(IPCGetModsResponse.Descriptor)) return [];

    return [.. response.Unpack<IPCGetModsResponse>().Mods];
}

public async Task<IPCModDetails?> GetModInformationAsync(string id)
{
    var message = new IPCGetModDetailsRequest()
    {
        Id = id
    };

    var response = await _gameFacade.RequestData(message);

    if (!response.Is(IPCGetModDetailsResponse.Descriptor)) return null;

    return response.Unpack<IPCGetModDetailsResponse>().Mod;
}

public string[] GetLoadedMods()
{
    if (_loadedMods is null) return [];

    return _loadedMods.Select(loadedMod => loadedMod.Key.Name).ToArray();
}

public async Task SetModUpdates(IPCSetManagedMods update)
{
    await _gameFacade.RequestData(update);
}

        //private readonly IGameFacade _gameFacade;

        //private readonly TaskCompletionSource<IPCGetCurrentManagedModsResponse> _managedMods = new();
*/