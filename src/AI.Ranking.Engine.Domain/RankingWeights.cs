using AI.Ranking.Engine.Domain.Exceptions;

namespace AI.Ranking.Engine.Domain;

/// <summary>
/// Configurable weights for hybrid scoring. Components are expected to sum to 1.0 for interpretable totals in [0, 1].
/// </summary>
public sealed record RankingWeights(
    double Semantic,
    double SkillOverlap,
    double ExperienceFit,
    double Keyword)
{
    public const double Tolerance = 1e-6;

    /// <summary>Default weights aligned with ADR 0002 (semantic + deterministic signals).</summary>
    public static RankingWeights Default { get; } = new(
        Semantic: 0.35,
        SkillOverlap: 0.35,
        ExperienceFit: 0.20,
        Keyword: 0.10);

    public void EnsureValid()
    {
        ValidateComponent(nameof(Semantic), Semantic);
        ValidateComponent(nameof(SkillOverlap), SkillOverlap);
        ValidateComponent(nameof(ExperienceFit), ExperienceFit);
        ValidateComponent(nameof(Keyword), Keyword);

        var sum = Semantic + SkillOverlap + ExperienceFit + Keyword;
        if (double.IsNaN(sum) || double.IsInfinity(sum))
            throw new InvalidRankingConfigurationException("Weight sum is not a finite number.");

        if (Math.Abs(sum - 1.0) > Tolerance)
        {
            throw new InvalidRankingConfigurationException(
                $"Ranking weights must sum to 1.0 (within {Tolerance}); actual sum is {sum:F8}.");
        }
    }

    private static void ValidateComponent(string name, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new InvalidRankingConfigurationException($"Weight '{name}' must be a finite number.");

        if (value < 0)
            throw new InvalidRankingConfigurationException($"Weight '{name}' cannot be negative.");
    }
}
