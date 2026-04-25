using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeIt.Models
{
    internal class CleanConfig
    {
        public bool FirstRun { get; set; } = true;
        public string Mode { get; set; } = "Safe";
        public List<FolderConfig> Folders { get; set; } = new List<FolderConfig>();
        public int MinFileSizeKB { get; set; } = 0;
        public double MaxDownloadSizeGB { get; set; } = 1.0;
        public double MaxQuarantineSizeGB { get; set; } = 2.0;
        public int CycleDays { get; set; } = 7;
        public bool Verbose { get; set; } = false;
        public bool DryRun { get; set; } = false;
    }

    internal class FolderConfig
        {
        public string Path { get; set; } = ""; //caminho da pasta
        public string Layer { get; set; } = "hard"; //camada de deleção entre hard soft ou manual
        public int MinAgeDays { get; set; } = 0; //min de dias sem acesso para o arquivo ser legícvel
        public bool Enabled { get; set; } = true; // se a pasta está habilitada ou não
        }
}
