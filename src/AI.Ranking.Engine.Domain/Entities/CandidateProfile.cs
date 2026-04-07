using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.Domain.Entities;

/// <summary>
/// Canonical candidate representation for ranking. Embedding storage is handled by vector recall; this type holds identity and structured features.
/// </summary>
public sealed class CandidateProfile
{
    public required string Id { get; init; }

    public required StructuredFeatures Features { get; init; }
}
