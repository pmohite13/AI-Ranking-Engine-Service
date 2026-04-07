namespace AI.Ranking.Engine.Application.Contracts;

/// <summary>
/// API contract for ranking a job against the candidate corpus (two-stage: vector recall + hybrid re-rank).
/// </summary>
public sealed record RankRequest(
    string JobId,
    int VectorRecallTopK,
    int FinalTopN);
