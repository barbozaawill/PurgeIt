using System.Runtime.InteropServices;

namespace PurgeIt.Core
{
    internal static class ConsoleHelper
    {
        // vai importar a função AllocConsole da kernel32.dll do Windows
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        //importa FreeConsole da kernel32.dll do Windows
        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        private static bool _isOpen = false;

        public static void Show()
        {
            if (_isOpen) return;
            AllocConsole();
            _isOpen = true;

            // Reconecta os streams do console após AllocConsole
            var stdOut = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(stdOut);

            var stdIn = new StreamReader(Console.OpenStandardInput());
            Console.SetIn(stdIn);

            Console.Title = "PurgeIt - Verbose";
            Console.WriteLine("=== PurgeIt - Modo Verbose ===");
            Console.WriteLine($"Iniciando em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            Console.WriteLine(new string('-', 40));
        }

        public static void Hide()
        {
            if (!_isOpen) return;

            Console.WriteLine(new string('-', 40));
            Console.WriteLine("Verbose encerrado. Pressione qualquer tecla para fechar.");
            Console.ReadKey();
            FreeConsole();
            _isOpen = false;
        }

        public static void Log(string message, bool verbose)
        {
            if (!verbose)
                return;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
