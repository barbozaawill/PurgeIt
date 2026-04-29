using PurgeIt.Services;
using PurgeIt.Models;
using PurgeIt.UI;

namespace PurgeIt
{
    public partial class MainForm : Form
    {
        private TrayIcon? _trayIcon;
        private System.Windows.Forms.Timer? _cleanTimer;
        private ConfigService? _configService;

        public MainForm()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Hide();
            CreateStartupShortcut();

            //vai iniciar os serviços
            var logService = new LogService();
            _configService = new ConfigService();
            var config = _configService.Load();

            //cria o ícone da bandeja
            _trayIcon = new TrayIcon(config, _configService, logService);

            double daysSinceLastClean = (DateTime.Now - config.LastCleanDate).TotalDays;

            if (config.FirstRun)
            {
                _trayIcon.RunCleanCycle(dryRun: true);
                config.FirstRun = false;
                config.LastCleanDate = DateTime.Now;
                _configService.Save(config);
            }
            else if (daysSinceLastClean >= config.CycleDays)
            {
                _trayIcon.RunCleanCycle();
                config.LastCleanDate = DateTime.Now;
                _configService.Save(config);
            }

            _cleanTimer = new System.Windows.Forms.Timer();
            _cleanTimer.Interval = 1000 * 60 * 60; // 1 hora em milissegundos
            _cleanTimer.Tick += OnTimerTick;
            _cleanTimer.Start();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            var config = _configService!.Load();
            double daysSinceLastClean = (DateTime.Now - config.LastCleanDate).TotalDays;

            if (daysSinceLastClean >= config.CycleDays)
            {
                _trayIcon!.RunCleanCycle();
                config.LastCleanDate = DateTime.Now;
                _configService.Save(config);
            }
        }

        private void CreateStartupShortcut()
        {
            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = Path.Combine(startupFolder, "PurgeIt.lnk");

                if (File.Exists(shortcutPath))
                    return;

                var shell = new IWshRuntimeLibrary.WshShell();
                var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = Application.ExecutablePath;
                shortcut.WorkingDirectory = Application.StartupPath;
                shortcut.Description = "PurgeIt - Limpador automático";
                shortcut.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao criar atalho de inicialização: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _cleanTimer?.Stop();
            _cleanTimer?.Dispose();
            _trayIcon?.Dispose();
        }
    }
}
