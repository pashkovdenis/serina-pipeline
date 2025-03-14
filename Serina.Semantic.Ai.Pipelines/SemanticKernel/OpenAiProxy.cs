namespace Serina.Semantic.Ai.Pipelines.SemanticKernel
{
    public class ProxyOpenAIHandler : HttpClientHandler
    {
        private string _proxyUrl;
        private string _apiKey;


        public ProxyOpenAIHandler()
        {

        }

        public ProxyOpenAIHandler(string proxyUrl, string apiKey)
        {
            _proxyUrl = proxyUrl;
            _apiKey = apiKey;

        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri != null && request.RequestUri.Host.Equals("api.openai.com", StringComparison.OrdinalIgnoreCase))
            {
                // your proxy url
                request.RequestUri = new Uri($"{_proxyUrl}{request.RequestUri.PathAndQuery}");
            }


            return base.SendAsync(request, cancellationToken);
        }
    }

}
