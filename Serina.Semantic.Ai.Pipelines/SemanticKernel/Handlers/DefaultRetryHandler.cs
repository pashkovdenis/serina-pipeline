using Polly;
using Polly.Retry;

namespace Serina.Semantic.Ai.Pipelines.SemanticKernel.Handlers
{
    public class SemanticKernelHandler : DelegatingHandler
    {
        private readonly SemanticKernelOptions _options;
        private readonly AsyncRetryPolicy retryPolicy;

        public SemanticKernelHandler(SemanticKernelOptions options, HttpMessageHandler innerHandler = null)
            : base(innerHandler ?? new HttpClientHandler())
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            retryPolicy = Policy
                .Handle<Exception>() // You can be more specific with the exception types
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), // Exponential back-off
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        // Log the retry attempt here.
                        Console.WriteLine($"Retry {retryCount} encountered error: {exception.Message}. Waiting {timespan.TotalSeconds} seconds before next retry.");
                    });
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                HttpResponseMessage responseMain = default;

                await retryPolicy.ExecuteAsync(async () =>
                {
                    responseMain = await base.SendAsync(request, cancellationToken);
                });

                if (responseMain.IsSuccessStatusCode)
                {
                    return responseMain;
                }

            }
            catch (Exception e)
            {
                // Log exception or continue to next model
                Console.WriteLine($"Fallback request failed: {e.Message}");
            }

            // Handle fallback logic if the primary model fails
            foreach (var model in _options.Models.Where(m => !string.IsNullOrEmpty(m.FallbackEndpoint)))
            {
                var originalRequestUri = request.RequestUri;

                // Modify request URI for fallback
                request.RequestUri = new Uri(model.FallbackEndpoint);

                // Replace authorization header for the fallback model
                request.Headers.Remove("Authorization");
                request.Headers.Add("Authorization", $"Bearer {model.Key}");

                // Re-send request with fallback endpoint
                try
                {
                    var response = await base.SendAsync(request, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                    }

                    response.Dispose();
                }
                catch (Exception ex)
                {
                    // Log exception or continue to next model
                    Console.WriteLine($"Fallback request failed: {ex.Message}");
                }
                finally
                {
                    // Reset the request URI to the original
                    request.RequestUri = originalRequestUri;
                }
            }

            throw new HttpRequestException("All models failed to process the request.");
        }
    }
}
