using AI.Ranking.Engine.Application.Ranking;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Entities;
using AI.Ranking.Engine.Domain.Scoring;

namespace AI.Ranking.Engine.UnitTests.Phase1;

public sealed class HybridRankingStrategyTests
{
    [Fact]
    public void ComputeScore_matches_domain_math()
    {
        var strategy = new HybridRankingStrategy();
        var candidate = new CandidateProfile
        {
            Id = "c1",
            Features = new StructuredFeatures(new[] { "a" }, 1, "t", null, null),
        };
        var job = new JobProfile
        {
            Id = "j1",
            Features = new StructuredFeatures(new[] { "a", "b" }, 0, "t", MinimumYears: 0, null),
        };

        var fromStrategy = strategy.ComputeScore(candidate, job, 0.8, RankingWeights.Default);
        var fromDomain = HybridRankingMath.Compute(candidate.Features, job.Features, 0.8, RankingWeights.Default);

        Assert.Equal(fromDomain.TotalScore, fromStrategy.TotalScore);
        Assert.Equal(fromDomain.Breakdown.TotalScore, fromStrategy.Breakdown.TotalScore);
    }
}
