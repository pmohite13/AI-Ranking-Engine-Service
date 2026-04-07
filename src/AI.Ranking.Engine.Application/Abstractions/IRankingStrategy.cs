using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Entities;

namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// Hybrid ranking strategy (semantic + deterministic). Pure scoring is implemented in <c>AI.Ranking.Engine.Domain.Scoring.HybridRankingMath</c>; this port supports DI and alternative strategies later.
/// </summary>
public interface IRankingStrategy
{
    RankingScore ComputeScore(
        CandidateProfile candidate,
        JobProfile job,
        double semanticSimilarity,
        RankingWeights weights);
}
