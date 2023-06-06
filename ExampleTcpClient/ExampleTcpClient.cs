using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ExampleTcpClient
{
    public class ExampleTcpClient
    {
        private readonly Socket _socket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly byte[] _buffer = new byte[BUFFER_SIZE];
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;
        private readonly ILogger _logger;

        private const int BUFFER_SIZE = 1048560;
        private const int CONNECTION_ATTEMPTS_LIMIT = 5;

        public bool IsConnected => _socket.Connected;
        public string? LocalEndPoint => _socket.LocalEndPoint?.ToString();


        public ExampleTcpClient(ILogger logger)
        {
            _logger = logger;
            _cancellationToken = _cancellationTokenSource.Token;
        }

        public void Connect(IPEndPoint endPoint)
        {
            int attempts = 0;

            while (!_socket.Connected)
            {
                attempts++;

                if (attempts >= CONNECTION_ATTEMPTS_LIMIT)
                {
                    _logger.Log("Connection attempts limit reached.");
                    return;
                }
                _logger.Log($"Connection attempt {attempts}...");

                try
                {
                    _logger.Log($"Connecting to {endPoint.Address}:{endPoint.Port}");

                    _socket.Connect(endPoint);

                    _logger.Log($"Connected! Local end point {_socket.LocalEndPoint}");


                    Task.Factory.StartNew(async () =>
                    {
                        while (!_cancellationToken.IsCancellationRequested && _socket.Connected)
                        {
                            await HandleIncomingData();
                        }
                    }, TaskCreationOptions.LongRunning);

                    _logger.Log("Running...");
                }
                catch (SocketException ex)
                {
                    _logger.Log($"Error: {ex.Message}");
                }
            }
        }

        async private Task HandleIncomingData()
        {
            try
            {
                _logger.Log($"Waiting for incoming data...");
                int bytesReceived = await _socket.ReceiveAsync(_buffer, SocketFlags.None, _cancellationToken);
                if (bytesReceived > 0)
                {
                    byte[] recBuf = new byte[bytesReceived];
                    Array.Copy(_buffer, recBuf, bytesReceived);
                    string receivedMessage = Encoding.ASCII.GetString(recBuf);
                    _logger.Log($"Received message: " + receivedMessage);
                }
                else if (bytesReceived == 0)
                {
                    Close();
                }
            }
            catch (OperationCanceledException) 
            {
                _logger.Log($"Socket.ReceiveAsync() Operation was canceled.");
            }
            catch (SocketException ex)
            {
                _logger.Log($"SocketException: {ex.Message}");
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        async public Task SendAsync(string message)
        {
            try
            {
                _logger.Log($"Sending message: \"{message}\"");
                byte[] data = Encoding.ASCII.GetBytes(message);

                int bytesSent = await _socket.SendAsync(data, SocketFlags.None, _cancellationToken);
                _logger.Log($"Message of size {bytesSent} sent.");
            }
            catch (OperationCanceledException)
            {
                _logger.Log($"Socket.SendAsync() Operation was canceled.");
            }
            catch (SocketException ex)
            {
                _logger.Log($"SocketException: {ex.Message}");
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        public void Close()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                _logger.Log($"Shutting down...");
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                _logger.Log($"Closed.");
            }
            catch (ObjectDisposedException)
            {
                _logger.Log($"Already closed!");
            }
            catch(Exception ex)
            {
                _logger.LogException(ex);
            }
        }
    }
}
