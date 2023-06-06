using System.Net;
using System.Runtime.InteropServices;

namespace ExampleTcpClient
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

        static private readonly ExampleTcpClient _tcpClient = new ExampleTcpClient(new CustomLogger("TCPCLIENT"));
        static private readonly IPEndPoint _defaultIPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 28015);
        static private readonly ILogger _logger = new CustomLogger("PROGRAM");

        static private readonly Dictionary<string, Action<string>> _consoleCommands = new Dictionary<string, Action<string>>()
        {
            { "send", (string commandLine) => {
                if (_tcpClient.IsConnected)
                {
                    _tcpClient.SendAsync(commandLine.Replace("send ", "")).Wait();
                }
                else
                {
                    _logger.Log("Socket is not connected!");
                }
            } },
            { "close", (string commandLine) => {
                if (_tcpClient.IsConnected)
                {
                    _tcpClient.Close();
                }
                else
                {
                    _logger.Log("Socket is not connected!");
                }
            } },
            { "help", (string commandLine) => {
                _logger.Log("Available commands:");
                _logger.Log("\tsend <message>\tSend message to server.");
                _logger.Log("\tclose\tClose socket connection.");
                _logger.Log("\texit\tExit.");
            } },
        };

        static void Main(string[] args)
        {
            DisableQuickEdit();

            Console.Title = "Tcp Client";
            _logger.Log("Type \"help\" to see available commands.");
            
            _tcpClient.Connect(_defaultIPEndPoint);

            if (_tcpClient.IsConnected)
            {
                Console.Title = $"Tcp Client {_tcpClient.LocalEndPoint}";

                string? input = Console.ReadLine();

                while (input != "exit")
                {
                    HandleCommand(input);

                    input = Console.ReadLine();
                }
            }

            Console.ReadKey();
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