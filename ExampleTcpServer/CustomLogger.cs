namespace ExampleTcpServer
{
    public class CustomLogger : ILogger
    {
        public string HolderName { get; set; } = "PROGRAM";
        public CustomLogger() { }
        public CustomLogger(string holderName)
        {
            HolderName = holderName;
        }

        public void Log(object? message)
        {
            Console.WriteLine($"{HolderName} :: {message ?? string.Empty}");
        }

        public void LogException(Exception ex)
        {
            Log($"ERROR :: MESSAGE: {ex.Message}");
            Log($"ERROR :: SOURCE: {ex.Source}");
            Log($"ERROR :: STACKTRACE: {ex.StackTrace}");
        }
    }
}
