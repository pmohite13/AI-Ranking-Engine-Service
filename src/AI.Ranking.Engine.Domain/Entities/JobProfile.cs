using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.Domain.Entities;

/// <summary>
/// Canonical job representation for ranking and embedding queries.
/// </summary>
public sealed class JobProfile
{
    public required string Id { get; init; }

    public required StructuredFeatures Features { get; init; }

    /// <summary>Optional normalized full text for embedding and diagnostics (not scored directly).</summary>
    public string? NormalizedDocumentText { get; init; }
}
