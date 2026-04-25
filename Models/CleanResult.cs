using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeIt.Models
{
    internal class CleanResult
    {
        public long TotalBytesFreed { get; set; } = 0;
        public int TotalFilesRemoved { get; set; } = 0;
        public int FilesQuarantined { get; set; } = 0;
        public long QuarantineSizeBytes { get; set; } = 0;
        public List<FileEntry> PendingManualConfirmation { get; set; } = new List<FileEntry>();
        public List<FileEntry> SkippedFiles { get; set; } = new List<FileEntry>();
        public TimeSpan ExecutionTime { get; set; }
        public bool WasDryRun { get; set; } = false;

        public double TotalMBFreed => Math.Round(TotalBytesFreed / 1024.0 / 1024.0, 2);
        public double TotalGBFreed => Math.Round(TotalGBFreed / 1024.0 / 1024.0 / 1024.0, 2);
        public double QuarantineSizeMB => Math.Round(QuarantineSizeMB / 1024.0 / 1024.0, 2);
    }
}
