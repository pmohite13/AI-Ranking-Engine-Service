namespace AI.Ranking.Engine.Application.Options;

/// <summary>
/// OpenAI structured extraction configuration for resume/job parsing.
/// </summary>
public sealed class LlmExtractionOptions
{
    public const string SectionName = "LlmExtraction";

    public string ApiKey { get; set; } = string.Empty;

    public Uri? Endpoint { get; set; }

    public string ModelId { get; set; } = "gpt-4o-mini";

    public int HttpClientTimeoutSeconds { get; set; } = 90;

    public int MaxInputCharacters { get; set; } = 12000;

    public TimeSpan? ExtractionCacheExpiration { get; set; } = TimeSpan.FromHours(6);

    public string ResolveApiKey()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
            return ApiKey;

        return Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
    }
}
