using PurgeIt.Models;
using PurgeIt.Services;

namespace PurgeIt.Core
{
    internal class Scanner
    {
        private readonly CleanConfig _config;
        private readonly RuleEngine _ruleEngine;
        private readonly LogService _logService;

        private static readonly List<(string Path, string Layer, int MinAgeDays, string MinMode)> KnownFolders = new()
        {
            (Environment.ExpandEnvironmentVariables("%TEMP%"),                          "hard",   0,  "Safe"),
            (@"C:\Windows\Temp",                                                        "hard",   0,  "Safe"),
            (Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Temp"),            "hard",   0,  "Safe"),
            (Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\CrashDumps"),      "hard",   0,  "Safe"),
            (GetBrowserCachePath("Chrome"),                                             "hard",   0,  "Safe"),
            (GetBrowserCachePath("Edge"),                                               "hard",   0,  "Safe"),
            (GetBrowserCachePath("Firefox"),                                            "hard",   0,  "Safe"),
            (GetDiscordCachePath(),                                                     "hard",   0,  "Safe"),
            (GetSteamPath("logs"),                                                      "hard",   0,  "Safe"),
            (GetSteamPath("appcache"),                                                  "hard",   0,  "Safe"),
            (GetSteamPath("depotcache"),                                                "hard",   0,  "Safe"),
            (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                +@"\Downloads",                                     "soft",   14, "Balanced"),
            (@"C:\$Recycle.Bin",                                                        "soft",   30, "Balanced"),
            (GetSteamPath("shadercache"),                                               "soft",   0,  "Balanced"),
            (@"C:\Windows\SoftwareDistribution\Download",                               "manual", 0,  "Aggressive"),
            (@"C:\Windows\Prefetch",                                                    "manual", 0,  "Aggressive"),
            (GetSteamPath("downloading"),                                               "manual", 0,  "Aggressive"),
        };

        public Scanner(CleanConfig config, RuleEngine ruleEngine, LogService logService)
        {
            _config = config;
            _ruleEngine = ruleEngine;
            _logService = logService;
        }

        public List<FileEntry> Scan()
        {
            var result = new List<FileEntry>();
            var foldersToScan = GetFoldersForCurrentMode();

            foreach (var folder in foldersToScan)
            {
                if (!Directory.Exists(folder.Path))
                    continue;

                try
                {
                    var files = Directory.GetFiles(folder.Path, "*", SearchOption.AllDirectories);

                    foreach ( string filePath in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(filePath);

                            var entry = new FileEntry
                            {
                                Path = filePath,
                                SizeBytes = fileInfo.Length,
                                Layer = folder.Layer,
                                LastAccessed = fileInfo.LastAccessTime,
                                Reason = GetReason(folder.Layer, folder.Path)
                            };

                            if (folder.MinAgeDays > 0)
                            {
                                double daysSinceAcess = (DateTime.Now - entry.LastAccessed).TotalDays;
                                if (daysSinceAcess < folder.MinAgeDays)
                                    continue;
                            }

                            var (allowed, reason) = _ruleEngine.Evaluate(entry);

                            ConsoleHelper.Log($"{(allowed ? "OK" : "SKIP")} | {reason} | {filePath}", _config.Verbose);

                            if (allowed)
                                result.Add(entry);
                                
                            else
                                _logService.LogSkipped(entry, reason);
                        }
                        catch (Exception ex)
                        {
                            _logService.LogError($"Erro ao escanear arquivo: {filePath}", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Erro ao escanear pasta: {folder.Path}", ex);
                }
            }

            return result;
        }

        private List<(string Path, string Layer, int MinAgeDays)> GetFoldersForCurrentMode()
        {
            var modes = new[] { "Safe", "Balanced", "Aggressive" };
            int currentModeIndex = Array.IndexOf(modes, _config.Mode);

            if (_config.Mode == "Custom")
            {
                return _config.Folders
                    .Where(f => f.Enabled)
                    .Select(f => (f.Path, f.Layer, f.MinAgeDays))
                    .ToList();
            }

            return KnownFolders
                .Where(f =>
                {
                    int folderModeIndex = Array.IndexOf(modes, f.MinMode);
                    return folderModeIndex <= currentModeIndex;
                })
                .Select(f => (f.Path, f.Layer, f.MinAgeDays))
                .ToList();
        }

        private string GetReason(string layer, string folderPath)
        {
            if (layer == "hard")
            {
                if (folderPath.Contains("Temp", StringComparison.OrdinalIgnoreCase))
                    return "tempFile";
                return "cacheFile";
            }
            if (layer == "soft")
                return "minAgeExceeded";

            return "userConfirmed";
        }

        private static string GetBrowserCachePath(string browser)
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return browser switch
            {
                "Chrome" => Path.Combine(local, "Google", "Chrome", "User Data", "Default", "Cache"),
                "Edge" => Path.Combine(local, "Microsoft", "Edge", "User Data", "Default", "Cache"),
                "Firefox" => Path.Combine(local, "Mozilla", "Firefox", "Profiles"),
                _ => ""
            };
        }

        private static string GetDiscordCachePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "discord", "Cache"
            );
        }
        private static string GetSteamPath(string subfolder)
        {
            return Path.Combine(@"C:\Program Files (x86)\Steam", subfolder);
        }
    }
}
