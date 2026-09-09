using System.Web;

namespace API.Handlers
{
    public class ORSApiKeyHandler : DelegatingHandler
    {
        private readonly IConfiguration _config;

        public ORSApiKeyHandler(IConfiguration config)
        {
            _config = config;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var apiKey = _config["OpenRouteService:ApiKey"]
                ?? throw new InvalidOperationException("OpenRouteService:ApiKey não configurada.");

            var uriBuilder = new UriBuilder(request.RequestUri!);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["api_key"] = apiKey;
            uriBuilder.Query = query.ToString();

            request.RequestUri = uriBuilder.Uri;

            return base.SendAsync(request, cancellationToken);
        }
    }
}
