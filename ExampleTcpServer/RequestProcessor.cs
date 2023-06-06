
namespace ExampleTcpServer
{
    public abstract class RequestProcessor
    {
        protected delegate string RequestCallback(string request);
        protected Dictionary<string, RequestCallback> _requestAcceptors = new Dictionary<string, RequestCallback>();

        public virtual bool TryGetResponse(string request, out string response)
        {
            response = string.Empty;

            foreach (var requestAcceptor in _requestAcceptors)
            {
                if (request.StartsWith(requestAcceptor.Key, StringComparison.OrdinalIgnoreCase))
                {
                    response = requestAcceptor.Value.Invoke(request);
                    return true;
                }
            }

            return false;
        }
    }
}
