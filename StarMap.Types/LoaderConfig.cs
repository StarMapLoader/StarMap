using System.Text.Json;

namespace StarMap.Types
{
    public class LoaderConfig
    {

        public bool TryLoadConfig()
        {
            if (!File.Exists("./StarMapConfig.json"))
            {
                Console.WriteLine("StarMap - Please fill the StarMapConfig.json and restart the program");
                File.WriteAllText("./StarMapConfig.json", JsonSerializer.Serialize(new LoaderConfig(), new JsonSerializerOptions { WriteIndented = true }));
                return false;
            }

            var jsonString = File.ReadAllText("./StarMapConfig.json");
            var config = JsonSerializer.Deserialize<LoaderConfig>(jsonString);

            if (config is null) return false;

            if (string.IsNullOrEmpty(config.GameLocation))
            {
                Console.WriteLine("StarMap - The 'GameLocation' property in StarMapConfig.json is either empty or points to a non-existing file.");
                return false;
            }

            string path = config.GameLocation;

            if (Directory.Exists(path))
            {
                path = Path.Combine(path, "KSA.dll");
            }

            if (!File.Exists(path))
            {
                Console.WriteLine("StarMap - Could not find KSA.dll. Make sure the folder or file path is correct:");
                Console.WriteLine(path);
                return false;
            }

            GameLocation = path;

            GameArguments = config.GameArguments;
            
            return true;
        }

        public string GameLocation { get; set; } = "";
        public string RepositoryLocation { get; set; } = "";
        public string[] GameArguments { get; set; } = [];
    }
}
