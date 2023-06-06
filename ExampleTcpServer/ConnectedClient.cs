using System.Net.Sockets;
using System.Text;

namespace ExampleTcpServer
{
    public class ConnectedClient
    {
        public string? RemoteEndPoint { get; }

        private readonly Socket _socket;
        private readonly byte[] _buffer = new byte[BUFFER_SIZE];
        private const int BUFFER_SIZE = 1048560;
        private readonly ILogger _logger;
        private readonly RequestProcessor _requestProcessor;

        public Action<ConnectedClient> OnSocketClosed;

        public ConnectedClient(Socket socket, RequestProcessor requestProcessor, ILogger logger)
        {
            _requestProcessor = requestProcessor;
            _logger = logger;
            _logger.Log($"Creating Client Connection...");
            _socket = socket;
            //_socket.BeginReceive(_buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, null);

            // Because Socket.RemoteEndPoint property is unaccessible after Socket Close
            RemoteEndPoint = _socket.RemoteEndPoint.ToString();

            Task.Factory.StartNew(async () =>
            {
                while (_socket.Connected) await HandleIncomingData();
            }, TaskCreationOptions.LongRunning);

            _logger.Log($"Client Connection is active.");
        }

        async private Task HandleIncomingData()
        {
            try
            {
                _logger.Log($"Waiting for incoming data...");

                int bytesReceived = await _socket.ReceiveAsync(_buffer, SocketFlags.None);

                if (bytesReceived > 0)
                {
                    _logger.Log($"Bytes received {bytesReceived}.");

                    byte[] recBuf = new byte[bytesReceived];
                    Array.Copy(_buffer, recBuf, bytesReceived);
                    string receivedMessage = Encoding.ASCII.GetString(recBuf);
                    _logger.Log($"Received message: " + receivedMessage);

                    if (_requestProcessor.TryGetResponse(receivedMessage, out string response))
                    {
                        SendAsync(response);
                    }
                }
                else if (bytesReceived == 0)
                {
                    Close();
                }
            }
            catch (SocketException)
            {
                _logger.Log("Socket closed the connection!");
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

                int bytesSent = await _socket.SendAsync(data, SocketFlags.None);
                _logger.Log($"Message of size {bytesSent} sent.");
            }
            catch (SocketException ex)
            {
                _logger.LogException(ex);
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogException(ex);
            }
        }

        public void Close()
        {
            try
            {
                _logger.Log($"Shutting down...");
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                _logger.Log($"Closed.");
            }
            catch (ObjectDisposedException)
            {
                _logger.Log($"Already closed!");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
            finally
            {
                _logger.Log($"Deleting Client Connection from connections list...");
                OnSocketClosed?.Invoke(this);
            }
        }
    }
}
