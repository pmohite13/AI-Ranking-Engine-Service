using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// Structured extraction from normalized resume/job text. Implementations must merge LLM output with heuristic fallback.
/// </summary>
public interface ILLMStructuredExtractor
{
    Task<StructuredFeatures> ExtractFromResumeAsync(string normalizedText, CancellationToken cancellationToken = default);

    Task<StructuredFeatures> ExtractFromJobAsync(string normalizedText, CancellationToken cancellationToken = default);
}
