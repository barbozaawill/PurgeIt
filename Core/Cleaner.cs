using PurgeIt.Models;
using PurgeIt.Services;

namespace PurgeIt.Core
{
    internal class Cleaner
    {
        private readonly CleanConfig _config;
        private readonly QuarantineService _quarantineService;
        private readonly LogService _logService;

        public Cleaner(CleanConfig config, QuarantineService quarantineService, LogService logService)
        {
            _config = config;
            _quarantineService = quarantineService;
            _logService = logService;
        }

        //limpeza na lista de arquivos e retorna o resultado do ciclo
        public CleanResult Execute(List<FileEntry> files)
        {
            var result = new CleanResult
            {
                WasDryRun = _config.DryRun
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            foreach (var file in files)
            {
                try
                {
                    if (file.Layer == "manual")
                    {
                        //arquivos manuais só vão para a lista de pendentes
                        result.PendingManualConfirmation.Add(file);
                        continue;
                    }

                    if (_config.DryRun)
                    {
                        //dry-run, só contabiliza sem apagar nada
                        result.TotalBytesFreed += file.SizeBytes;
                        result.TotalFilesRemoved++;
                        file.Processed = true;

                        var dryEntry = new FileEntry
                        {
                            Path = file.Path,
                            SizeBytes = file.SizeBytes,
                            Layer = file.Layer,
                            Reason = "dryRun",
                            LastAccessed = file.LastAccessed
                        };
                        _logService.LogFile(dryEntry, "DRYRUN");
                        continue;
                    }

                    if (file.Layer == "hard")
                    {
                        DeleteHard(file, result);
                    }
                    else if (file.Layer == "soft")
                    {
                        DeleteSoft(file, result);
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Erro ao processar arquivo: {file.Path}", ex);
                    result.SkippedFiles.Add(file);
                }
            }

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
            result.QuarantineSizeBytes = _quarantineService.GetQuarantineSizeBytes();

            _logService.LogResult(result);

            return result;
        }

        //apaga o arquivo permanentemente
        private void DeleteHard(FileEntry file, CleanResult result)
        {
            if (!File.Exists(file.Path))
            {
                result.SkippedFiles.Add(file);
                return;
            }

            File.Delete(file.Path);
            file.Processed = true;

            result.TotalBytesFreed += file.SizeBytes;
            result.TotalFilesRemoved++;

            _logService.LogFile(file, "DELETE");
        }

        //move o arquivo para quarentena
        private void DeleteSoft(FileEntry file, CleanResult result)
        {
            if (!File.Exists(file.Path))
            {
                result.SkippedFiles.Add(file);
                return;
            }

            bool moved = _quarantineService.MoveToQuarantine(file);

            if (moved)
            {
                file.Processed = true;
                result.TotalBytesFreed += file.SizeBytes;
                result.FilesQuarantined++;
            }
            else
            {
                result.SkippedFiles.Add(file);
            }
        }

        //executa a exclusão de um arquivo pendente de confirmação manual
        public bool DeleteManual(FileEntry file)
        {
            try
            {
                if (!File.Exists(file.Path))
                    return false;

                File.Delete(file.Path);
                file.Processed = true;
                _logService.LogFile(file, "DELETE_MANUAL");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Erro ao apagar arquivo manual: {file.Path}", ex);
                return false;
            }
        }
    }
}