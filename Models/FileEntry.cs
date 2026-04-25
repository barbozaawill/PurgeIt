using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeIt.Models
{
    internal class FileEntry
    {
        public string Path { get; set; } = "";
        public long SizeBytes { get; set; } = 0;
        public string Layer { get; set; } = "hard";
        public string Reason { get; set; } = "";
        public DateTime LastAccessed { get; set; }
        public bool Processed { get; set; } = false;
        public double SizeMB => Math.Round(SizeBytes / 1024.0 / 1024.0, 2);
    }
}
