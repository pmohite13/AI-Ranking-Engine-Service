using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.Infrastructure.Extraction;

/// <summary>
/// Structured extraction service with LLM-first extraction, heuristic fallback, validation, and cache.
/// </summary>
public sealed class OpenAiStructuredExtractor : ILLMStructuredExtractor
{
    private readonly IStructuredLlmClient _llmClient;
    private readonly ICacheService _cache;
    private readonly IOptions<LlmExtractionOptions> _options;
    private readonly HeuristicStructuredFeatureExtractor _heuristicExtractor;
    private readonly ILogger<OpenAiStructuredExtractor> _logger;

    public OpenAiStructuredExtractor(
        IStructuredLlmClient llmClient,
        ICacheService cache,
        IOptions<LlmExtractionOptions> options,
        HeuristicStructuredFeatureExtractor heuristicExtractor,
        ILogger<OpenAiStructuredExtractor> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _heuristicExtractor = heuristicExtractor ?? throw new ArgumentNullException(nameof(heuristicExtractor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<StructuredFeatures> ExtractFromResumeAsync(string normalizedText, CancellationToken cancellationToken = default) =>
        ExtractInternalAsync(normalizedText, ExtractionDocumentKind.Resume, cancellationToken);

    public Task<StructuredFeatures> ExtractFromJobAsync(string normalizedText, CancellationToken cancellationToken = default) =>
        ExtractInternalAsync(normalizedText, ExtractionDocumentKind.Job, cancellationToken);

    private async Task<StructuredFeatures> ExtractInternalAsync(
        string normalizedText,
        ExtractionDocumentKind kind,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
            return StructuredFeatures.Empty;

        var cfg = _options.Value;
        var safeText = normalizedText.Length > cfg.MaxInputCharacters
            ? normalizedText[..cfg.MaxInputCharacters]
            : normalizedText;

        var cacheKey = LlmExtractionCacheKeyBuilder.Build(cfg.ModelId, kind, safeText);
        var cached = await _cache.GetAsync<StructuredFeatures>(cacheKey, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        var fallback = _heuristicExtractor.Extract(safeText, kind);
        var llmPayload = await _llmClient.ExtractAsync(safeText, kind, cancellationToken).ConfigureAwait(false);
        var merged = MergeAndValidate(llmPayload, fallback, kind);

        await _cache.SetAsync(cacheKey, merged, cfg.ExtractionCacheExpiration, cancellationToken).ConfigureAwait(false);
        return merged;
    }

    private StructuredFeatures MergeAndValidate(
        StructuredExtractionPayload? llmPayload,
        StructuredFeatures fallback,
        ExtractionDocumentKind kind)
    {
        if (llmPayload is null)
            return fallback;

        var skills = NormalizeSkillList(llmPayload.Skills);
        if (skills.Count == 0)
            skills = fallback.Skills;

        var roleTitle = NormalizeRoleTitle(llmPayload.RoleTitle) ?? fallback.NormalizedRoleTitle;
        var years = ClampYears(llmPayload.YearsExperience) ?? fallback.YearsExperience;
        var minYears = ClampYears(llmPayload.MinimumYears) ?? fallback.MinimumYears;
        var maxYears = ClampYears(llmPayload.MaximumYears) ?? fallback.MaximumYears;

        if (kind == ExtractionDocumentKind.Resume)
        {
            // Resume extraction should not produce hard min/max requirements.
            minYears = null;
            maxYears = null;
        }
        else if (minYears is not null && maxYears is not null && minYears > maxYears)
        {
            _logger.LogDebug("LLM extraction returned invalid min/max years; falling back to heuristic range.");
            minYears = fallback.MinimumYears;
            maxYears = fallback.MaximumYears;
        }

        return new StructuredFeatures(
            Skills: skills,
            YearsExperience: years,
            NormalizedRoleTitle: roleTitle,
            MinimumYears: minYears,
            MaximumYears: maxYears);
    }

    private static IReadOnlyList<string> NormalizeSkillList(IReadOnlyList<string>? skills)
    {
        if (skills is null || skills.Count == 0)
            return Array.Empty<string>();

        var set = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < skills.Count; i++)
        {
            var token = NormalizeSkillToken(skills[i]);
            if (token.Length > 0)
                set.Add(token);
        }

        return set.Count == 0 ? Array.Empty<string>() : set.ToArray();
    }

    private static string NormalizeSkillToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var skill = value.Trim().ToLowerInvariant();
        return skill switch
        {
            "c sharp" => "c#",
            ".net core" => ".net",
            "dot net" => "dotnet",
            _ => skill,
        };
    }

    private static string? NormalizeRoleTitle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var title = value.Trim().ToLowerInvariant();
        return title.Length == 0 ? null : title;
    }

    private static double? ClampYears(double? years)
    {
        if (years is null || double.IsNaN(years.Value) || double.IsInfinity(years.Value))
            return null;

        if (years < 0)
            return 0;

        if (years > 60)
            return 60;

        return years.Value;
    }
}
