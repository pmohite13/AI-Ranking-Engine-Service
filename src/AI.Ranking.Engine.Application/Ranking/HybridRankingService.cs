using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Entities;
using AI.Ranking.Engine.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.Application.Ranking;

/// <summary>
/// Executes two-stage retrieval: vector recall Top-K followed by hybrid score re-ranking.
/// </summary>
public sealed class HybridRankingService : IHybridRankingService
{
    private readonly IVectorRecall _vectorRecall;
    private readonly IRankingStrategy _rankingStrategy;
    private readonly IOptions<RankingWeights> _weightsOptions;

    public HybridRankingService(
        IVectorRecall vectorRecall,
        IRankingStrategy rankingStrategy,
        IOptions<RankingWeights> weightsOptions)
    {
        _vectorRecall = vectorRecall ?? throw new ArgumentNullException(nameof(vectorRecall));
        _rankingStrategy = rankingStrategy ?? throw new ArgumentNullException(nameof(rankingStrategy));
        _weightsOptions = weightsOptions ?? throw new ArgumentNullException(nameof(weightsOptions));
    }

    public async Task<IReadOnlyList<RankingResult>> RankTopCandidatesAsync(
        JobProfile job,
        float[] jobEmbedding,
        IReadOnlyDictionary<string, CandidateProfile> candidateProfiles,
        int vectorRecallTopK,
        int finalTopN,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(jobEmbedding);
        ArgumentNullException.ThrowIfNull(candidateProfiles);

        if (candidateProfiles.Count == 0)
            throw new EmptyCandidateCorpusException("Candidate corpus is empty; cannot rank.");

        if (vectorRecallTopK <= 0)
            throw new ArgumentOutOfRangeException(nameof(vectorRecallTopK), "Vector recall Top-K must be greater than zero.");

        if (finalTopN <= 0)
            throw new ArgumentOutOfRangeException(nameof(finalTopN), "Final Top-N must be greater than zero.");

        if (finalTopN > vectorRecallTopK)
        {
            throw new ArgumentException(
                "Final Top-N must be less than or equal to vector recall Top-K.",
                nameof(finalTopN));
        }

        var weights = _weightsOptions.Value;
        weights.EnsureValid();

        var recallHits = await _vectorRecall
            .SearchTopKAsync(jobEmbedding, vectorRecallTopK, cancellationToken)
            .ConfigureAwait(false);

        if (recallHits.Count == 0)
            return Array.Empty<RankingResult>();

        var scored = new List<(string CandidateId, RankingScore Score)>(recallHits.Count);
        foreach (var hit in recallHits)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!candidateProfiles.TryGetValue(hit.CandidateId, out var candidate))
                continue;

            var score = _rankingStrategy.ComputeScore(candidate, job, hit.Similarity, weights);
            scored.Add((hit.CandidateId, score));
        }

        if (scored.Count == 0)
            return Array.Empty<RankingResult>();

        scored.Sort(static (a, b) =>
        {
            var scoreCmp = b.Score.TotalScore.CompareTo(a.Score.TotalScore);
            if (scoreCmp != 0)
                return scoreCmp;

            return string.CompareOrdinal(a.CandidateId, b.CandidateId);
        });

        var count = Math.Min(finalTopN, scored.Count);
        var results = new List<RankingResult>(count);
        for (var i = 0; i < count; i++)
        {
            var row = scored[i];
            results.Add(new RankingResult
            {
                CandidateId = row.CandidateId,
                Rank = i + 1,
                TotalScore = row.Score.TotalScore,
                Breakdown = row.Score.Breakdown,
            });
        }

        return results;
    }
}
