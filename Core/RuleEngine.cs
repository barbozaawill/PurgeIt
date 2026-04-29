using PurgeIt.Models;

namespace PurgeIt.Core
{
    internal class RuleEngine
    {
        private static readonly HashSet<string> BlockedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
                @"C:\",
                @"C:\Windows",
                @"C:\Users",
                @"C:\Program Files",
                @"C:\Program Files (x86)",
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetEnvironmentVariable("SYSTEMDRIVE") ?? @"C:\"
        };

        private static readonly HashSet<string> InstallerExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".msi", ".msix"
        };

        private static readonly HashSet<string> ActiveDownloadExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".crdownload", ".part"
        };

        private readonly CleanConfig _config;
        public RuleEngine(CleanConfig config)
        {
            _config = config;
        }

        public (bool allowed, string reason) Evaluate(FileEntry file)
        {
            if (IsInBlockedPath(file.Path))
                return (false, "blockedPath");

            if (!File.Exists(file.Path))
                return (false, "fileNotFound");

            if (IsFileLocked(file.Path))
                return (false, "fileLocked");

            if (HasProtectedAttributes(file.Path))
                return (false, "protectedAttributes");

            if (Path.GetExtension(file.Path).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                return (false, "isShortcut");

            if (_config.MinFileSizeKB > 0 && file.SizeBytes < _config.MinFileSizeKB * 1024L)
                return (false, "belowMinSize"); 

            if (IsActiveDownload(file.Path))
                return (false, "activeDownload");

            if (IsRecentInstaller(file))
                return (false, "recentInstaller");

            return (true, "");
        }

        private bool IsInBlockedPath(string filePath)
        {
            string normalizedFile = Path.GetFullPath(filePath);

            foreach (string blocked in BlockedPaths)
            {
                string normalizedBlocked = Path.GetFullPath(blocked);

                if (normalizedFile.Equals(normalizedBlocked, StringComparison.OrdinalIgnoreCase))
                    return true;

                string? parentFolder = Path.GetDirectoryName(normalizedFile);
                if (parentFolder != null && parentFolder.Equals(normalizedBlocked, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private bool IsFileLocked(string filePath) 
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool HasProtectedAttributes(string filePath)
        {
            try
            {
                var attributes = File.GetAttributes(filePath);
                return attributes.HasFlag(FileAttributes.ReadOnly) ||
                       attributes.HasFlag(FileAttributes.System);
            }
            catch
            {
                return false;
            }
        }

        private bool IsActiveDownload(string filePath)
        {
            string ext = Path.GetExtension(filePath);
            return ActiveDownloadExtensions.Contains(ext);
        }

        private bool IsRecentInstaller(FileEntry file)
        {
            string ext = Path.GetExtension(file.Path);
            if (!InstallerExtensions.Contains(ext))
                return false;

            double daysSinceAccess = (DateTime.Now - file.LastAccessed).TotalDays;
            return daysSinceAccess < 30;
        }
    }
}
