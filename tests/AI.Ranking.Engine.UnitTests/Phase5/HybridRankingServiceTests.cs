using AI.Ranking.Engine.Application.Ranking;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Entities;
using AI.Ranking.Engine.Infrastructure.VectorRecall;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.UnitTests.Phase5;

public sealed class HybridRankingServiceTests
{
    [Fact]
    public async Task RankTopCandidatesAsync_returns_top_n_from_recall_then_hybrid_rerank()
    {
        var recall = new InMemoryVectorRecall();
        await recall.UpsertCandidateAsync("c1", new float[] { 1, 0, 0 });
        await recall.UpsertCandidateAsync("c2", new float[] { 0.8f, 0.2f, 0 });
        await recall.UpsertCandidateAsync("c3", new float[] { 0.2f, 0.8f, 0 });

        var candidates = new Dictionary<string, CandidateProfile>(StringComparer.Ordinal)
        {
            ["c1"] = NewCandidate("c1", new[] { "c#", ".net", "sql" }, 6, "senior backend engineer"),
            ["c2"] = NewCandidate("c2", new[] { "java", "spring" }, 4, "backend developer"),
            ["c3"] = NewCandidate("c3", new[] { "react", "typescript" }, 5, "frontend engineer"),
        };

        var job = new JobProfile
        {
            Id = "job-1",
            Features = new StructuredFeatures(
                Skills: new[] { "c#", ".net", "sql" },
                YearsExperience: 0,
                NormalizedRoleTitle: "senior backend engineer",
                MinimumYears: 5,
                MaximumYears: 8),
        };

        var service = new HybridRankingService(
            recall,
            new HybridRankingStrategy(),
            Options.Create(RankingWeights.Default));

        var results = await service.RankTopCandidatesAsync(
            job,
            jobEmbedding: new float[] { 1, 0, 0 },
            candidateProfiles: candidates,
            vectorRecallTopK: 3,
            finalTopN: 2);

        Assert.Equal(2, results.Count);
        Assert.Equal("c1", results[0].CandidateId);
        Assert.Equal("c2", results[1].CandidateId);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal(2, results[1].Rank);
        Assert.True(results[0].TotalScore >= results[1].TotalScore);
    }

    private static CandidateProfile NewCandidate(string id, IReadOnlyList<string> skills, double years, string title) =>
        new()
        {
            Id = id,
            Features = new StructuredFeatures(skills, years, title, null, null),
        };
}
