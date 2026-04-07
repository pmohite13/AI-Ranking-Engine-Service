using AI.Ranking.Engine.Application.Ranking;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Entities;
using AI.Ranking.Engine.Domain.Scoring;
using AI.Ranking.Engine.Infrastructure.VectorRecall;

namespace AI.Ranking.Engine.UnitTests.Phase4;

/// <summary>
/// Recall similarity (dot on L2-normalized vectors) matches the semantic channel used by <see cref="HybridRankingMath"/>.
/// </summary>
public sealed class VectorRecallHybridConsistencyTests
{
    [Fact]
    public async Task Recall_similarity_matches_hybrid_semantic_channel()
    {
        var recall = new InMemoryVectorRecall();
        var candidateEmbedding = new float[] { 1f, 0f, 0f };
        await recall.UpsertCandidateAsync("c1", candidateEmbedding);

        var jobQuery = new float[] { 1f, 0f, 0f };
        var hits = await recall.SearchTopKAsync(jobQuery, k: 1);
        var semanticFromRecall = hits[0].Similarity;

        var candidate = new CandidateProfile
        {
            Id = "c1",
            Features = StructuredFeatures.Empty,
        };

        var job = new JobProfile
        {
            Id = "j1",
            Features = StructuredFeatures.Empty,
        };

        var strategy = new HybridRankingStrategy();
        var score = strategy.ComputeScore(candidate, job, semanticFromRecall, new RankingWeights(Semantic: 1, 0, 0, 0));

        Assert.InRange(score.Breakdown.SemanticRaw, (double)semanticFromRecall - 1e-5, (double)semanticFromRecall + 1e-5);
        Assert.InRange(score.TotalScore, 0.99, 1.01);
    }
}
