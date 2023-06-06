using System.Runtime.InteropServices;

namespace ExampleTcpServer
{
    internal class Program
    {
        private const uint ENABLE_QUICK_EDIT = 0x0040;

        // STD_INPUT_HANDLE (DWORD): -10 is the standard input device.
        private const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        static private extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static private extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static private extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        static private readonly ExampleTcpServer _tcpServer = new ExampleTcpServer(new CustomLogger("TCPSERVER"));
        static private readonly ILogger _logger = new CustomLogger("PROGRAM");

        static private readonly Dictionary<string, Action<string>> _consoleCommands = new Dictionary<string, Action<string>>()
        {
            { "close", (string commandLine) => {
                _tcpServer.Close();
            } },
            { "broadcast", (string commandLine) => {
                _tcpServer.Broadcast(commandLine.Replace("broadcast ", ""));
            } },
            { "clients", (string commandLine) => {
                string[] clients = _tcpServer.ClientEndPoints;
                _logger.Log("Connected clients:");
                for (int i = 0; i < clients.Length; i++)
                {
                    _logger.Log($"\t{i}: {clients[i]}");
                }
            } },
            { "help", (string commandLine) => {
                _logger.Log("Available commands:");
                _logger.Log("\tclose\t\t\tCloses TcpServer.");
                _logger.Log("\tclients\t\t\tList connected clients.");
                _logger.Log("\tbroadcast <message>\tSend message to all clients.");
                _logger.Log("\texit\t\t\tExit.");
            } },
        };

        static void Main(string[] args)
        {
            DisableQuickEdit();

            Console.Title = "Tcp Server";
            _logger.Log("Type \"help\" to see available commands.");
            
            _tcpServer.Start();

            string? input = Console.ReadLine();
            while (input != "exit")
            {
                HandleCommand(input);

                input = Console.ReadLine();
            }
        }

        static private void HandleCommand(string? commandLine)
        {
            if (string.IsNullOrEmpty(commandLine)) return;

            foreach (var consoleCommand in _consoleCommands)
            {
                if (commandLine.StartsWith(consoleCommand.Key, StringComparison.OrdinalIgnoreCase))
                {
                    consoleCommand.Value.Invoke(commandLine);

                    return;
                }
            }

            _logger.Log("Unknown command!");
        }

        static private bool DisableQuickEdit()
        {
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            uint consoleMode;
            if (!GetConsoleMode(consoleHandle, out consoleMode))
            {
                return false;
            }

            consoleMode &= ~ENABLE_QUICK_EDIT;

            if (!SetConsoleMode(consoleHandle, consoleMode))
            {
                return false;
            }

            return true;
        }
    }
}