using PurgeIt.Core;
using PurgeIt.Models;
using PurgeIt.Services;

namespace PurgeIt.UI
{
    internal class TrayIcon : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly CleanConfig _config;
        private readonly ConfigService _configService;
        private readonly LogService _logService;

        public TrayIcon(CleanConfig config, ConfigService configService, LogService logService)
        {
            _config = config;
            _configService = configService;
            _logService = logService;

            _contextMenu = BuildContextMenu();

            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon(Path.Combine(Application.StartupPath, "Resources", "black1000.ico")),
                Visible = true,
                ContextMenuStrip = _contextMenu,
                Text = "PurgeIt"
            };

            _notifyIcon.DoubleClick += OnDoubleClick;
        }

        private void SetMode(string mode)
        {
            if (mode == "Aggressive")
            {
                var warningForm = new AggressiveWarningForm();
                warningForm.ShowDialog();
                if (!warningForm.Confirmed)
                    return;
            }

            var config = _configService.Load();
            config.Mode = mode;
            _configService.Save(config);

            ShowNotification("PurgeIt", $"Modo alterado para {mode}");
        }

        //monta o menu de contexto
        private ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            var itemCleanNow = new ToolStripMenuItem("Limpar agora");
            itemCleanNow.Click += OnCleanNow;

            var itemDryRun = new ToolStripMenuItem("Simular limpeza (Dry Run)");
            itemDryRun.Click += OnDryRun;

            var itemOpenLog = new ToolStripMenuItem("Ver log");
            itemOpenLog.Click += OnOpenLog;

            var itemSelectMode = new ToolStripMenuItem("Modo de limpeza");

            var itemSafe = new ToolStripMenuItem("Safe");
            var itemBalanced = new ToolStripMenuItem("Balanced");
            var itemAggressive = new ToolStripMenuItem("Aggressive");

            var currentConfig = _configService.Load();
            switch (currentConfig.Mode)
            {
                case "Safe": itemSafe.Checked = true; break;
                case "Balanced": itemBalanced.Checked = true; break;
                case "Aggressive": itemAggressive.Checked = true; break;
            }

            itemSafe.Click += (s, e) => SetMode("Safe");
            itemBalanced.Click += (s, e) => SetMode("Balanced");
            itemAggressive.Click += (s, e) => SetMode("Aggressive");

            itemSelectMode.DropDownItems.AddRange(new ToolStripItem[]
            {
                itemSafe, itemBalanced, itemAggressive
            });

            var itemSeparator = new ToolStripSeparator();

            var itemExit = new ToolStripMenuItem("Sair");
            itemExit.Click += OnExit;

            menu.Items.AddRange(new ToolStripItem[]
            {
                itemCleanNow,
                itemDryRun,
                itemOpenLog,
                itemSelectMode,
                itemSeparator,
                itemExit
            });

            return menu;
        }

        //double click para abrir o painel das métricas 
        private void OnDoubleClick(object? sender, EventArgs e)
        {
            ShowNotification("PurgeIt", "Clique com o botão direito para ver as opções.");
        }

        // "limpar agora"
        private void OnCleanNow(object? sender, EventArgs e)
        {
            try
            {
                RunCleanCycle(dryRun: false);
            }
            catch (Exception ex)
            {
                _logService.LogError("Erro ao iniciar limpeza", ex);
                ShowNotification("PurgeIt - Erro", "Ocorreu um erro durante a limpeza. Consulte o log.");
            }
        }

        //faz a simulação da limpeza
        private void OnDryRun(object? sender, EventArgs e)
        {
            try
            {
                RunCleanCycle(dryRun: true);
            }
            catch (Exception ex)
            {
                _logService.LogError("Erro ao iniciar simulação de limpeza", ex);
            }
        }

        private void OnOpenLog(object? sender, EventArgs e)
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PurgeIt",
                "purge.log"
            );

            if (File.Exists(logPath))
                System.Diagnostics.Process.Start("notepad.exe", logPath);
            else
                ShowNotification("PurgeIt - Log", "Nenhum arquivo de log encontrado.");
        }

        private void OnExit(object? sender, EventArgs e)
        {
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        public void RunCleanCycle(bool dryRun = false)
        {
            var config = _configService.Load();
            config.DryRun = dryRun;

            if (config.Verbose)
            {
                ConsoleHelper.Show();
            }

            if (config.Mode == "Aggressive")
            {
                var warningForm = new AggressiveWarningForm();
                warningForm.ShowDialog();

                if (!warningForm.Confirmed)
                {
                    if (config.Verbose)
                    {
                        ConsoleHelper.Hide();
                    }
                    return;
                }
                    
            }

            var logService = new LogService();
            var configService = new ConfigService();
            var quarantineService = new QuarantineService(logService);
            var ruleEngine = new RuleEngine(config);
            var scanner = new Scanner(config, ruleEngine, logService);
            var cleaner = new Cleaner(config, quarantineService, logService);

            //apaga os arquivos expirados da quarentena antes de iniciar
            quarantineService.PurgeExpired();

            var files = scanner.Scan();

            var result = cleaner.Execute(files);

            double quarantineGB = quarantineService.GetQuarantineSizeGB();
            if (quarantineGB > config.MaxQuarantineSizeGB)
                ShowNotification("PurgeIt - Quarentena", $"A pasta de quarentena ultrapassou {config.MaxQuarantineSizeGB}GB.");

            if (config.DryRun)
            {
                ShowNotification("PurgeIt - Simulação", $"PurgeIt pode liberar {result.TotalGBFreed}GB no seu sistema.");
            }
            else
            {
                string message = $"Limpeza concluída. {result.TotalGBFreed}GB liberados, {result.TotalFilesRemoved} arquivos removidos."; 

                if (result.PendingManualConfirmation.Count >0)
                    message += $"\n{result.PendingManualConfirmation.Count} arquivo(s) aguardando confirmãção manual.";

                ShowNotification("PurgeIt", message);

                if (result.PendingManualConfirmation.Count > 0)
                {
                    var confirmForm = new ConfirmationForm(result.PendingManualConfirmation, logService);
                    confirmForm.Show();
                }
            }

            config.LastCleanDate = DateTime.Now;
            _configService.Save(config);

            if (config.Verbose)
            {
                ConsoleHelper.Hide();
            }
        }

        public void ShowNotification(string title, string message) 
        {
            _notifyIcon.ShowBalloonTip(
                timeout: 4000,
                tipTitle: title,
                tipText: message,
                tipIcon: ToolTipIcon.Info
            );
        }

        public void Dispose()
        {
            _notifyIcon.Dispose();
            _contextMenu.Dispose();
        }
    }
}
