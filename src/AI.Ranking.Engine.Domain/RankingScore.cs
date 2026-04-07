namespace AI.Ranking.Engine.Domain;

/// <summary>
/// Result of scoring one candidate against one job (before ordering / Top-N truncation).
/// </summary>
public sealed record RankingScore(double TotalScore, ScoreBreakdown Breakdown);
