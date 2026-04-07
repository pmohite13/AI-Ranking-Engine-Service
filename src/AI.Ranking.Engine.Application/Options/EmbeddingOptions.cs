namespace AI.Ranking.Engine.Application.Options;

/// <summary>
/// OpenAI embeddings configuration. API key may be supplied here or via <c>OPENAI_API_KEY</c> environment variable.
/// </summary>
public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    /// <summary>Optional explicit API key; if empty, <see cref="ResolveApiKey"/> uses environment.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Optional OpenAI API base URL (e.g. proxy); default is the official endpoint.</summary>
    public Uri? Endpoint { get; set; }

    /// <summary>Default embedding model id when callers use a single shared model.</summary>
    public string DefaultModelId { get; set; } = "text-embedding-3-small";

    /// <summary>Target vector dimensions (e.g. 1536 for text-embedding-3-small). Use 0 to let the model use its default.</summary>
    public int DefaultDimensions { get; set; } = 1536;

    /// <summary>Hard cap on estimated tokens per input (OpenAI embedding models typically allow 8191).</summary>
    public int MaxTokensPerInput { get; set; } = 8191;

    /// <summary>Maximum number of input strings per single embeddings HTTP request.</summary>
    public int MaxInputsPerHttpRequest { get; set; } = 512;

    /// <summary>Conservative ceiling on total estimated tokens per HTTP request (batch budget).</summary>
    public int MaxEstimatedTokensPerHttpRequest { get; set; } = 250_000;

    /// <summary>Heuristic: approximate characters per token for batching (English-heavy text).</summary>
    public int EstimatedCharsPerToken { get; set; } = 4;

    /// <summary>Optional sliding expiration for cached vectors.</summary>
    public TimeSpan? EmbeddingCacheExpiration { get; set; }

    /// <summary>Per-request HTTP client timeout (seconds) for OpenAI embedding calls.</summary>
    public int HttpClientTimeoutSeconds { get; set; } = 120;

    /// <summary>Resolves API key from options or <c>OPENAI_API_KEY</c>.</summary>
    public string ResolveApiKey()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
            return ApiKey;

        return Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
    }
}
