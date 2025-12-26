using StarMap.Index.API;
using StarMap.Types.Proto.IPC;
using System.Text.Json;

namespace StarMapLoader
{
    internal class ModRepository
    {
        public List<IPCMod> LoadedModInformation { get; private set; } = [];
        public bool HasChanges { get; private set; }

        private readonly string _modsPath;
        private readonly IModRespositoryClient _foreignModRepository;
        private readonly ModDownloader _downloader = new();

        private IPCUpdateModInformation[] _changes = [];


        public ModRepository(string gameLocation, IModRespositoryClient foreignModRepository)
        {
            var gameDirectory = Path.GetDirectoryName(gameLocation);
            if (string.IsNullOrEmpty(gameDirectory))
            {
                throw new ArgumentException("Invalid game location provided, unable to determine game directory.");
            }

            _modsPath = Path.Combine(gameDirectory, "mods");
            _foreignModRepository = foreignModRepository;

            if (!Directory.Exists(_modsPath))
            {
                Directory.CreateDirectory(_modsPath);
            }

            var filePath = Path.Combine(_modsPath, "starmap.json");
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Dispose();
                File.WriteAllText(filePath, JsonSerializer.Serialize(new List<IPCMod>()));
            }

            string jsonString = File.ReadAllText(Path.Combine(_modsPath, "starmap.json"));

            LoadedModInformation = JsonSerializer.Deserialize<List<IPCMod>>(jsonString) ?? [];
        }

        public async Task<Mod[]> GetPossibleMods()
        {
            return await _foreignModRepository.GetMods();
        }

        public async Task<ModDetails?> GetModInformation(string modId)
        {
            return await _foreignModRepository.GetModDetails(Guid.Parse(modId));
        }

        public void SetModUpdates(IPCUpdateModInformation[] updates)
        {
            _changes = updates;
            HasChanges = true;
        }

        public void ApplyModUpdates()
        {
            HasChanges = false;

            foreach (var modChange in _changes)
            {
                try
                {
                    var directoryPath = Path.Combine(_modsPath, modChange.Mod.Name);

                    if (Directory.Exists(directoryPath))
                    {
                        Directory.Delete(directoryPath, true);
                    }

                    Directory.CreateDirectory(directoryPath);

                    if (!_downloader.DownloadMod(modChange.Mod, modChange.AfterVersion, directoryPath) && modChange.BeforeVersion is not null)
                    {
                        Directory.Delete(directoryPath, true);
                        Directory.CreateDirectory(directoryPath);
                        _downloader.DownloadMod(modChange.Mod, modChange.BeforeVersion, directoryPath);
                    }
                    else
                    {
                        var newModInfo = new IPCMod()
                        {
                            Id = modChange.Mod.Id,
                            Name = modChange.Mod.Name,
                            Version = modChange.AfterVersion.Version
                        };

                        var index = LoadedModInformation.FindIndex(modInfo => modInfo.Id == newModInfo.Id);

                        if (index >= 0)
                        {
                            LoadedModInformation[index] = newModInfo;
                        }
                        else
                        {
                            LoadedModInformation.Add(newModInfo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"StarMap - Unable to apply update for mod: {modChange.Mod.Name} to version {modChange.AfterVersion?.Version ?? "<Version not filled in>"}: {ex}");
                }
            }

            File.WriteAllText(Path.Combine(_modsPath, "starmap.json"), JsonSerializer.Serialize(LoadedModInformation));
        }
    }
}
