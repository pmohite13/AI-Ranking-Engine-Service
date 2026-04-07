namespace AI.Ranking.Engine.Domain;

/// <summary>
/// Explainable decomposition of a hybrid score. Raw values are in [0, 1] before weights; weighted* are contributions to <see cref="TotalScore"/>.
/// </summary>
public sealed record ScoreBreakdown(
    double SemanticRaw,
    double SkillOverlapRaw,
    double ExperienceFitRaw,
    double KeywordRaw,
    double WeightedSemantic,
    double WeightedSkillOverlap,
    double WeightedExperienceFit,
    double WeightedKeyword,
    double TotalScore);
