using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AI.Ranking.Engine.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.Infrastructure.Extraction;

internal sealed class OpenAiStructuredLlmClient : IStructuredLlmClient
{
    public const string HttpClientName = "OpenAI.StructuredExtraction";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<LlmExtractionOptions> _options;
    private readonly ILogger<OpenAiStructuredLlmClient> _logger;

    public OpenAiStructuredLlmClient(
        IHttpClientFactory httpClientFactory,
        IOptions<LlmExtractionOptions> options,
        ILogger<OpenAiStructuredLlmClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StructuredExtractionPayload?> ExtractAsync(
        string normalizedText,
        ExtractionDocumentKind kind,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
            return null;

        var cfg = _options.Value;
        var apiKey = cfg.ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var client = _httpClientFactory.CreateClient(HttpClientName);
        var endpoint = cfg.Endpoint?.ToString()?.TrimEnd('/') ?? "https://api.openai.com";
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = BuildRequest(cfg.ModelId, normalizedText, kind);
        request.Content = JsonContent.Create(body, options: JsonOptions);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Structured extraction call failed with status {Status}.", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            var json = payload?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<StructuredExtractionPayload>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Structured extraction call failed.");
            return null;
        }
    }

    private static ChatCompletionRequest BuildRequest(string modelId, string normalizedText, ExtractionDocumentKind kind)
    {
        var schema = """
{
  "type": "object",
  "properties": {
    "skills": { "type": "array", "items": { "type": "string" } },
    "yearsExperience": { "type": "number" },
    "roleTitle": { "type": "string" },
    "minimumYears": { "type": "number" },
    "maximumYears": { "type": "number" }
  },
  "additionalProperties": false
}
""";

        var kindPrompt = kind == ExtractionDocumentKind.Resume
            ? "resume text"
            : "job description text";

        return new ChatCompletionRequest(
            Model: modelId,
            ResponseFormat: new ResponseFormat("json_schema", new JsonSchemaContainer("candidate_job_features", schema, true)),
            Messages:
            [
                new ChatMessage("system", "Extract structured hiring features in strict JSON. Use null when unknown."),
                new ChatMessage("user", $"Extract fields from this {kindPrompt}:\n\n{normalizedText}"),
            ]);
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("response_format")] ResponseFormat ResponseFormat,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ResponseFormat(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("json_schema")] JsonSchemaContainer JsonSchema);

    private sealed record JsonSchemaContainer(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("schema")] string Schema,
        [property: JsonPropertyName("strict")] bool Strict);

    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] List<ChatChoice>? Choices);

    private sealed record ChatChoice(
        [property: JsonPropertyName("message")] ChatMessageResponse? Message);

    private sealed record ChatMessageResponse(
        [property: JsonPropertyName("content")] string? Content);
}
