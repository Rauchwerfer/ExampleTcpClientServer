using System.Net;
using System.Net.Sockets;

namespace ExampleTcpServer
{
    public class ExampleTcpServer
    {
        private const int PORT = 28015;

        private readonly Socket _socket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly List<ConnectedClient> _connectedClients = new List<ConnectedClient>();
        private readonly ILogger _logger;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;

        public string[] ClientEndPoints => _connectedClients.Select(c => c.RemoteEndPoint).ToArray();
        public ExampleTcpServer(ILogger logger)
        {
            _cancellationToken = _cancellationTokenSource.Token;
            _logger = logger;
        }

        public void Start(int port = PORT)
        {
            _connectedClients.Clear();

            var endPoint = new IPEndPoint(IPAddress.Any, port);
            _logger.Log($"Starting server on {endPoint}...");
            _socket.Bind(endPoint);
            _socket.Listen(0);

            Task.Factory.StartNew(async () =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                { 
                    await HandleIncomingConnections(); 
                }
            }, TaskCreationOptions.LongRunning);
            _logger.Log("Server is running.");
        }

        private async Task HandleIncomingConnections()
        {
            _logger.Log("Waiting for incoming connections...");
            var clientSocket = await _socket.AcceptAsync(_cancellationToken);
            _logger.Log($"Connection from {clientSocket.RemoteEndPoint}...");

            var connectedClient = new ConnectedClient(
                clientSocket,
                new ClientRequestProcessor(),
                new CustomLogger($"TCPCLIENT {clientSocket.RemoteEndPoint}")
                );

            connectedClient.OnSocketClosed = (connectedClient) => 
            {                
                if (_connectedClients.Remove(connectedClient))
                {
                    _logger.Log("Closed client connection was successfully removed.");
                }
                else
                {
                    _logger.Log("Closed client connection was not found in connections list.");
                }
            };

            _connectedClients.Add(connectedClient);
        }

        public void Close()
        {
            List<Action> closeClientActions = new List<Action>();
            
            foreach (var connectedClient in _connectedClients)
            {
                closeClientActions.Add(connectedClient.Close);
            }

            foreach (var closeClientAction in closeClientActions)
            {
                closeClientAction?.Invoke();
            }

            try
            {
                _logger.Log($"Shutting down...");
                //_socket.Shutdown(SocketShutdown.Both);
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
        }

        async public void Broadcast(string message)
        {
            List<Task> _sendMessageTasks = new List<Task>();
            
            foreach (var connectedClient in _connectedClients)
            {
                _sendMessageTasks.Add(connectedClient.SendAsync(message));
            }

            await Task.WhenAll(_sendMessageTasks);
            _logger.Log("Message broadcasting is done.");
        }
    }
}
