using PurgeIt.Models;

namespace PurgeIt.Services
{
    internal class QuarantineService
    {
        private static readonly string QuarantinePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PurgeIt",
            "Quarantine"
        );

        private const int QuarantineDays = 2;

        private readonly LogService _logService;

        public QuarantineService(LogService logService)
        {
            _logService = logService;
        }

        public bool MoveToQuarantine(FileEntry file)
        {
            try
            {
                if (!Directory.Exists(QuarantinePath))
                    Directory.CreateDirectory(QuarantinePath);

                string fileName = Path.GetFileName(file.Path);
                string destination = Path.Combine(QuarantinePath, fileName);

                if (File.Exists(destination))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string nameWithout = Path.GetFileNameWithoutExtension(file.Path);
                    string ext = Path.GetExtension(file.Path);
                    destination = Path.Combine(QuarantinePath, $"{nameWithout}_{timestamp}{ext}");
                }

                File.Move(file.Path, destination);
                _logService.LogFile(file, "QUARANTINE");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Falha ao mover para quarentena {file.Path}", ex);
                return false;
            }
        }

        public int PurgeExpired()
        {
            if (!Directory.Exists(QuarantinePath))
                return 0;

            int count = 0;
            var files = Directory.GetFiles(QuarantinePath);

            foreach (string filePath in files)
            {
                try
                {
                    DateTime lastWrite = File.GetLastWriteTime(filePath);
                    bool expired = (DateTime.Now - lastWrite).TotalDays >= QuarantineDays;

                    if (expired)
                    {
                        File.Delete(filePath);
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Falha ao purgar arquivo de quarentena {filePath}", ex);
                }
            }

            return count;
        }

        public long GetQuarantineSizeBytes()
        {
            if (!Directory.Exists(QuarantinePath))
                return 0;

            return Directory.GetFiles(QuarantinePath)
                .Sum(f => new FileInfo(f).Length);
        }

        public double GetQuarantineSizeGB()
        {
            return Math.Round(GetQuarantineSizeBytes() / (1024.0 * 1024.0 * 1024.0), 2);
        }
    }
}
