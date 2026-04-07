using System.Net;
using System.Net.Http;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Infrastructure.Extraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace AI.Ranking.Engine.Infrastructure.Http;

internal static class OpenAiStructuredExtractionHttpClientExtensions
{
    public static IHttpClientBuilder AddOpenAiStructuredExtractionHttpClient(this IServiceCollection services)
    {
        return services
            .AddHttpClient(OpenAiStructuredLlmClient.HttpClientName, ConfigureClient)
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
    }

    private static void ConfigureClient(IServiceProvider sp, HttpClient client)
    {
        var opts = sp.GetRequiredService<IOptions<LlmExtractionOptions>>().Value;
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, opts.HttpClientTimeoutSeconds));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(static r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                3,
                static retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
