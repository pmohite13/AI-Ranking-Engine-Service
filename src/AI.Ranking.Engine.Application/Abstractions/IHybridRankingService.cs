using AI.Ranking.Engine.Domain.Entities;

namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// Orchestrates two-stage ranking: vector recall Top-K then hybrid re-ranking.
/// </summary>
public interface IHybridRankingService
{
    Task<IReadOnlyList<RankingResult>> RankTopCandidatesAsync(
        JobProfile job,
        float[] jobEmbedding,
        IReadOnlyDictionary<string, CandidateProfile> candidateProfiles,
        int vectorRecallTopK,
        int finalTopN,
        CancellationToken cancellationToken = default);
}
