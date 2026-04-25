namespace PurgeIt
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            using var mutex = new Mutex(true, "PurgeIt_SingleInstance", out bool isNewInstance);

            if (!isNewInstance)
            {
                MessageBox.Show(
                    "O PurgeIt já está em execução.",
                    "PurgeIt",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            Application.Run(new MainForm());
        }
    }
}