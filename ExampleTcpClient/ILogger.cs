namespace ExampleTcpClient
{
    public interface ILogger
    {
        public void Log(object? message);
        public void LogException(Exception ex);
    }
}
