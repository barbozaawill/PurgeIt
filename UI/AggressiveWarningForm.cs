namespace PurgeIt.UI
{
    internal partial class AggressiveWarningForm : Form
    {
        // Retorna true se o usuário confirmou, false se cancelou
        public bool Confirmed { get; private set; } = false;

        public AggressiveWarningForm()
        {
            InitializeComponent();
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "PurgeIt - Atenção";
            this.Size = new Size(520, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblIcon = new Label
            {
                Text = "⚠️",
                Font = new Font("Segoe UI", 24f),
                Left = 20,
                Top = 20,
                Width = 50,
                Height = 50
            };

            var lblTitle = new Label
            {
                Text = "Modo Aggressive — Leia antes de continuar",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Left = 75,
                Top = 28,
                Width = 420,
                Height = 30
            };

            // Descrição dos riscos
            var lblDescription = new Label
            {
                Text =
                    "O modo Aggressive inclui pastas que podem afetar o funcionamento do sistema:\n\n" +
                    "• SoftwareDistribution\\Download — Arquivos de atualização do Windows. " +
                    "Apagar pode forçar o re-download de atualizações pendentes.\n\n" +
                    "• Prefetch — Dados usados pelo Windows para acelerar a abertura de programas. " +
                    "Apagar pode deixar o sistema mais lento temporariamente.\n\n" +
                    "• Steam\\downloading — Downloads em andamento do Steam. " +
                    "Apagar interrompe downloads ativos.",
                Left = 20,
                Top = 75,
                Width = 470,
                Height = 220,
                Font = new Font("Segoe UI", 9f)
            };

            // Checkbox de confirmação
            var chkConfirm = new CheckBox
            {
                Text = "Li e assumo a responsabilidade",
                Left = 20,
                Top = 305,
                Width = 270,
                Height = 24,
                Font = new Font("Segoe UI", 9.5f),
                Name = "chkConfirm",
                Enabled = false
            };

            var btnContinue = new Button
            {
                Text = "Continuar",
                Width = 100,
                Height = 34,
                Left = 290,
                Top = 300,
                Enabled = false,
                Name = "btnContinue"
            };
            btnContinue.Click += OnContinue;

            var btnCancel = new Button
            {
                Text = "Cancelar",
                Width = 100,
                Height = 34,
                Left = 400,
                Top = 300
            };
            btnCancel.Click += (s, e) => this.Close();

            var timer = new System.Windows.Forms.Timer { Interval = 5000 };
            timer.Tick += (s, e) =>
            {
                chkConfirm.Enabled = true;
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();

            // Habilita o botão Continuar só quando o checkbox for marcado
            chkConfirm.CheckedChanged += (s, e) =>
            {
                btnContinue.Enabled = chkConfirm.Checked;
            };

            this.Controls.AddRange(new Control[]
            {
                lblIcon, lblTitle, lblDescription, chkConfirm, btnContinue, btnCancel
            });
        }

        private void OnContinue(object? sender, EventArgs e)
        {
            Confirmed = true;
            this.Close();
        }
    }
}