namespace AI.Ranking.Engine.Domain;

/// <summary>
/// Structured features extracted from resume or job text (LLM + heuristic fallback).
/// Skills are expected to be pre-normalized (e.g. lower-invariant, trimmed) by the extraction layer.
/// </summary>
public sealed record StructuredFeatures(
    IReadOnlyList<string> Skills,
    double YearsExperience,
    string? NormalizedRoleTitle,
    /// <summary>When set (typically for job profiles), experience fit compares candidate years against this range.</summary>
    double? MinimumYears = null,
    double? MaximumYears = null)
{
    public static StructuredFeatures Empty { get; } = new(
        Array.Empty<string>(),
        YearsExperience: 0,
        NormalizedRoleTitle: null,
        MinimumYears: null,
        MaximumYears: null);
}
