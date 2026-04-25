using PurgeIt.Models;

namespace PurgeIt.Services
{
    internal class LogService
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PurgeIt",
            "purge.log"
        );

        public void LogFile(FileEntry file, string action)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                           $"ACTION={action} | " +
                           $"LAYER={file.Layer} | " +
                           $"REASON={file.Reason} | " +
                           $"SIZE={file.SizeMB}MB | " +
                           $"PATH={file.Path}";

            WriteToLog(entry);
        }

        public void LogResult(CleanResult result)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                           $"CYCLE COMPLETE | " +
                           $"FREED={result.TotalGBFreed}GB | " +
                           $"FILES={result.TotalFilesRemoved} | " +
                           $"QUARANTINED={result.FilesQuarantined} | " +
                           $"PENDING={result.PendingManualConfirmation.Count} | " +
                           $"DURATION={result.ExecutionTime.TotalSeconds:F2}s | " +
                           $"DRYRUN={result.WasDryRun}";

            WriteToLog(entry);
        }

        public void LogSkipped(FileEntry file, string reason)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]" +
                            $"SKIPPED | " +
                            $"REASON={reason} | " +
                            $"PATH={file.Path}";
            WriteToLog(entry);
        }

        public void LogError(string message, Exception? ex = null)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                           $"ERROR: {message}";

            if (ex != null)
                entry += $" | EXCEPTION={ex.Message}";

            WriteToLog(entry);
        }

        private void WriteToLog(string entry)
        {
            string? folder = Path.GetDirectoryName(LogPath);
            if (folder != null && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            File.AppendAllText(LogPath, entry + Environment.NewLine);
        }
    }
}
