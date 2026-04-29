using PurgeIt.Services;
using PurgeIt.Models;

namespace PurgeIt.UI
{
    internal partial class ConfirmationForm : Form
    {
        private readonly List<FileEntry> _pendingFiles;
        private readonly LogService _logService;
        public ConfirmationForm(List<FileEntry> pendingFiles, LogService logService)
        {
            InitializeComponent();
            _pendingFiles = pendingFiles;
            _logService = logService;

            BuildUI();
            PopulateList();
        }

        private void BuildUI()
        {
            this.Text = "PurgeIt - Confirmação Manual";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(600, 400);

            var lblTitle = new Label
            {
                Text = "Os arquivos abaixo requerem confirmação manual antes de serem apagados.",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Font = new Font("Segoe UI", 9.5F)
            };

            var listView = new ListView
            {
                View = View.Details,
                Dock = DockStyle.Fill,
                GridLines = true,
                FullRowSelect = true,
                Name = "listViewFiles"
            };

            listView.Columns.Add("Arquivo", 350);
            listView.Columns.Add("Tamanho", 100);
            listView.Columns.Add("Último acesso", 150);

            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(8)
            };

            var btnDeleteAll = new Button
            {
                Text = "Apagar Todos",
                Width = 120,
                Height = 34,
                Left = 8,
                Top = 8
            };
            btnDeleteAll.Click += OnDeleteAll;

            var btnDeleteOne = new Button
            {
                Text = "Apagar Selecionado",
                Width = 140,
                Height = 34,
                Left = 136,
                Top = 8
            };
            btnDeleteOne.Click += OnDeleteSelected;

            var btnExportAndClose = new Button
            {
                Text = "Exportar e Fechar",
                Width = 130,
                Height = 34,
                Left = 284,
                Top = 8
            };
            btnExportAndClose.Click += OnExportAndClose;

            var btnClose = new Button
            {
                Text = "Fechar",
                Width = 80,
                Height = 34,
                Left = 422,
                Top = 8
            };
            btnClose.Click += (s, e) => this.Close();

            panel.Controls.AddRange(new Control[] { btnDeleteAll, btnDeleteOne, btnExportAndClose, btnClose });

            this.Controls.Add(listView);
            this.Controls.Add(panel);
            this.Controls.Add(lblTitle);
        }

        private void PopulateList()
        {
            var listView = (ListView)this.Controls["listViewFiles"]!;

            foreach (var file in _pendingFiles)
            {
                var item = new ListViewItem(file.Path);
                item.SubItems.Add(file.SizeMB + "MB");
                item.SubItems.Add(file.LastAccessed.ToString("dd/MM/yyyy"));
                item.Tag = file;
                listView.Items.Add(item);
            }
        }

        private void OnDeleteAll(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                $"Tem certeza que deseja apagar {_pendingFiles.Count} arquivo(s) permanentemente?",
                "PurgeIt - Confirmação",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes)
                return;

            int deleted = 0;
            foreach (var file in _pendingFiles)
            {
                try
                {
                    if (File.Exists(file.Path))
                    {
                        File.Delete(file.Path);
                        _logService.LogFile(file, "DELETE_MANUAL");
                        deleted++;
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Erro ao apagar arquivo manual: {file.Path}", ex);
                }
            }

            MessageBox.Show($"{deleted} arquivo(s) apagado(s) com sucesso.", "PurgeIt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private void OnDeleteSelected(object? sender, EventArgs e)
        {
            var listView = (ListView)this.Controls["listViewFiles"]!;

            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Selecione um arquivo na lista.", "PurgeIt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedItem = listView.SelectedItems[0];
            var file = (FileEntry)selectedItem.Tag!;

            var confirm = MessageBox.Show(
                $"Apagar permanentemente?\n{file.Path}",
                "PurgeIt - Confirmação",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                if (File.Exists(file.Path))
                {
                    File.Delete(file.Path);
                    _logService.LogFile(file, "DELETE_MANUAL");
                    listView.Items.Remove(selectedItem);
                    MessageBox.Show("Arquivo apagado com sucesso.", "PurgeIt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Erro ao apagar arquivo manual: {file.Path}", ex);
                MessageBox.Show("Erro ao apagar o arquivo. Consulte o log.", "PurgeIt", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnExportAndClose(object? sender, EventArgs e)
        {
            foreach (var file in _pendingFiles)
            {
                var exportEntry = new FileEntry
                {
                    Path = file.Path,
                    SizeBytes = file.SizeBytes,
                    Layer = file.Layer,
                    Reason = "pendingManual",
                    LastAccessed = file.LastAccessed
                };
                _logService.LogFile(exportEntry, "EXPORTED");
            }

            MessageBox.Show(
                $"{_pendingFiles.Count} arquivo(s) exportado(s) para o log.",
                "PurgeIt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            this.Close();
        }

    }
}
