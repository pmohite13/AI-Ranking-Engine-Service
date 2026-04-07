using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Domain.Exceptions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;

namespace AI.Ranking.Engine.Infrastructure.Embeddings;

/// <summary>
/// OpenAI embeddings via official SDK, using <see cref="IHttpClientFactory" /> for the HTTP transport (Polly policies on the named client).
/// </summary>
public sealed class OpenAIEmbeddingClient : IEmbeddingClient
{
    public const string HttpClientName = "OpenAI.Embeddings";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<EmbeddingOptions> _options;
    private readonly ILogger<OpenAIEmbeddingClient> _logger;
    private readonly ConcurrentDictionary<string, EmbeddingClient> _clients = new(StringComparer.Ordinal);

    public OpenAIEmbeddingClient(
        IHttpClientFactory httpClientFactory,
        IOptions<EmbeddingOptions> options,
        ILogger<OpenAIEmbeddingClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<EmbeddingVector>> EmbedAsync(
        IReadOnlyList<string> texts,
        EmbeddingRequestOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);
        ArgumentNullException.ThrowIfNull(options);

        if (texts.Count == 0)
            return Array.Empty<EmbeddingVector>();

        var settings = _options.Value;
        var apiKey = settings.ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ExternalServiceException("OpenAI API key is not configured. Set Embedding:ApiKey or OPENAI_API_KEY.");

        var modelId = string.IsNullOrWhiteSpace(options.ModelId) ? settings.DefaultModelId : options.ModelId.Trim();
        var http = _httpClientFactory.CreateClient(HttpClientName);
        var embeddingClient = GetOrCreateEmbeddingClient(modelId, apiKey, http);

        var batches = EmbeddingRequestBatchPlanner.Plan(
            texts,
            settings.MaxInputsPerHttpRequest,
            settings.MaxTokensPerInput,
            settings.MaxEstimatedTokensPerHttpRequest,
            settings.EstimatedCharsPerToken);

        var generationOptions = BuildGenerationOptions(options);
        var results = new List<EmbeddingVector>(texts.Count);

        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ClientResult<OpenAIEmbeddingCollection> response;
            try
            {
                response = await embeddingClient
                    .GenerateEmbeddingsAsync(batch, generationOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI embeddings request failed for model {Model}.", modelId);
                throw new ExternalServiceException("OpenAI embeddings request failed.", ex);
            }

            var collection = response.Value;
            var usage = collection.Usage;
            var perItemTokens = usage is not null && batch.Count > 0
                ? (int?)Math.Max(1, usage.TotalTokenCount / batch.Count)
                : null;

            var count = 0;
            foreach (var item in collection)
            {
                var floats = item.ToFloats();
                var array = floats.ToArray();
                results.Add(new EmbeddingVector(array, perItemTokens));
                count++;
            }

            if (count != batch.Count)
            {
                throw new ExternalServiceException(
                    $"OpenAI returned {count} embedding(s) but {batch.Count} were requested.");
            }
        }

        if (results.Count != texts.Count)
        {
            throw new ExternalServiceException(
                $"Embedding result count ({results.Count}) does not match input count ({texts.Count}).");
        }

        return results;
    }

    private EmbeddingClient GetOrCreateEmbeddingClient(string modelId, string apiKey, HttpClient http)
    {
        var settings = _options.Value;
        var cacheKey = $"{modelId}\u001F{settings.Endpoint?.AbsoluteUri ?? "default"}";
        return _clients.GetOrAdd(cacheKey, _ =>
        {
            var openAiOptions = new OpenAIClientOptions();
            if (settings.Endpoint is not null)
                openAiOptions.Endpoint = settings.Endpoint;
            openAiOptions.NetworkTimeout = TimeSpan.FromSeconds(Math.Max(5, settings.HttpClientTimeoutSeconds));
            openAiOptions.Transport = new HttpClientPipelineTransport(http);
            return new EmbeddingClient(modelId, new ApiKeyCredential(apiKey), openAiOptions);
        });
    }

    private static EmbeddingGenerationOptions BuildGenerationOptions(EmbeddingRequestOptions options)
    {
        var gen = new EmbeddingGenerationOptions();
        if (options.Dimensions > 0)
            gen.Dimensions = options.Dimensions;
        return gen;
    }
}
