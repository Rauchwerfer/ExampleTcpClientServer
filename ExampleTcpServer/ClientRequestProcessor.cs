namespace ExampleTcpServer
{
    public class ClientRequestProcessor : RequestProcessor
    {
        public ClientRequestProcessor()
        {
            _requestAcceptors.Add(
                "gettime", (string request) => {
                    return DateTime.Now.ToString();
                } );
            _requestAcceptors.Add(
                "ping", (string request) => {
                    return "pong";
                });
        }
    }
}
