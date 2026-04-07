namespace AI.Ranking.Engine.Application.Options;

/// <summary>
/// Upper bounds for ranking API parameters (recall breadth and final Top-N).
/// </summary>
public sealed class RankingConstraintsOptions
{
    public const string SectionName = "Ranking";

    public int MaxVectorRecallTopK { get; set; } = 500;

    public int MaxFinalTopN { get; set; } = 100;
}
