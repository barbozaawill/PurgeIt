
namespace PurgeIt
{
    public partial class MainForm : Form
    {
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
        }
    }
}
