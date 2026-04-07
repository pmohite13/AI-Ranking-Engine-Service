using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Entities;
using AI.Ranking.Engine.Domain.Scoring;

namespace AI.Ranking.Engine.Application.Ranking;

/// <summary>
/// Default hybrid ranker delegating to pure domain math.
/// </summary>
public sealed class HybridRankingStrategy : IRankingStrategy
{
    public RankingScore ComputeScore(
        CandidateProfile candidate,
        JobProfile job,
        double semanticSimilarity,
        RankingWeights weights)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(job);

        return HybridRankingMath.Compute(candidate.Features, job.Features, semanticSimilarity, weights);
    }
}
