using System.Text.Json;
using PurgeIt.Models;

namespace PurgeIt.Services
{
    internal class ConfigService
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PurgeIt",
            "config.json"
        );

        public CleanConfig Load()
        {
            if (!File.Exists(ConfigPath))
            {
                var defaultConfig = new CleanConfig();
                Save(defaultConfig);
                return defaultConfig;
            }

            string json = File.ReadAllText(ConfigPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<CleanConfig>(json) ?? new CleanConfig();
        }

        public void Save(CleanConfig config)
        {
            string? folder = Path.GetDirectoryName(ConfigPath);
            if (folder != null && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ConfigPath, json);
        }
    }
}
