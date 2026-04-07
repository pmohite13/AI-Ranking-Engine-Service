using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Exceptions;
using AI.Ranking.Engine.Domain.Scoring;

namespace AI.Ranking.Engine.UnitTests.Phase1;

public sealed class HybridRankingMathTests
{
    private static readonly RankingWeights UnitWeights = new(1.0, 0, 0, 0);

    [Fact]
    public void Compute_perfect_alignment_yields_total_one_with_default_weights()
    {
        var skills = new[] { "csharp", "sql" };
        var candidate = new StructuredFeatures(skills, YearsExperience: 5, NormalizedRoleTitle: "senior engineer", null, null);
        var job = new StructuredFeatures(skills, YearsExperience: 0, NormalizedRoleTitle: "senior engineer", MinimumYears: 3, MaximumYears: 10);

        var score = HybridRankingMath.Compute(candidate, job, semanticSimilarity: 1.0, RankingWeights.Default);

        Assert.InRange(score.TotalScore, 0.99, 1.01);
        Assert.InRange(score.Breakdown.SemanticRaw, 0.99, 1.01);
        Assert.InRange(score.Breakdown.SkillOverlapRaw, 0.99, 1.01);
        Assert.InRange(score.Breakdown.ExperienceFitRaw, 0.99, 1.01);
        Assert.InRange(score.Breakdown.KeywordRaw, 0.99, 1.01);
    }

    [Fact]
    public void Higher_skill_overlap_scores_higher_when_semantic_fixed()
    {
        var weights = new RankingWeights(Semantic: 0, SkillOverlap: 1, ExperienceFit: 0, Keyword: 0);
        var job = new StructuredFeatures(new[] { "a", "b", "c" }, 0, "role", null, null);

        var low = HybridRankingMath.Compute(
            new StructuredFeatures(new[] { "a" }, 0, null, null, null),
            job,
            0.5,
            weights);

        var high = HybridRankingMath.Compute(
            new StructuredFeatures(new[] { "a", "b" }, 0, null, null, null),
            job,
            0.5,
            weights);

        Assert.True(high.TotalScore > low.TotalScore);
    }

    [Fact]
    public void Experience_fit_penalizes_under_minimum()
    {
        var weights = new RankingWeights(0, 0, 1, 0);
        var job = new StructuredFeatures(Array.Empty<string>(), 0, null, MinimumYears: 10, MaximumYears: null);

        var low = HybridRankingMath.Compute(
            new StructuredFeatures(Array.Empty<string>(), 2, null, null, null),
            job,
            1.0,
            weights);

        var ok = HybridRankingMath.Compute(
            new StructuredFeatures(Array.Empty<string>(), 10, null, null, null),
            job,
            1.0,
            weights);

        Assert.True(ok.TotalScore > low.TotalScore);
    }

    [Fact]
    public void SkillOverlap_Jaccard_matches_domain_helper()
    {
        var j = HybridRankingMath.SkillOverlap(new[] { "x", "y" }, new[] { "y", "z" });
        Assert.Equal(1.0 / 3.0, j, 5);
    }

    [Fact]
    public void ExperienceFit_rejects_non_finite_candidate_years()
    {
        Assert.Throws<DomainException>(() =>
            HybridRankingMath.ExperienceFit(double.NaN, null, null));
    }

    [Fact]
    public void Compute_is_deterministic()
    {
        var c = new StructuredFeatures(new[] { "go" }, 1, "dev", null, null);
        var j = new StructuredFeatures(new[] { "go", "rust" }, 0, "lead dev", MinimumYears: 0, null);
        var w = RankingWeights.Default;

        var a = HybridRankingMath.Compute(c, j, 0.7, w);
        var b = HybridRankingMath.Compute(c, j, 0.7, w);

        Assert.Equal(a.TotalScore, b.TotalScore);
        Assert.Equal(a.Breakdown.TotalScore, b.Breakdown.TotalScore);
    }

    [Fact]
    public void Semantic_input_is_clamped_to_unit_interval()
    {
        var c = StructuredFeatures.Empty;
        var j = StructuredFeatures.Empty;
        var w = UnitWeights;

        var high = HybridRankingMath.Compute(c, j, semanticSimilarity: 2.0, w);
        var neg = HybridRankingMath.Compute(c, j, semanticSimilarity: -1.0, w);

        Assert.Equal(1.0, high.Breakdown.SemanticRaw, 5);
        Assert.Equal(0.0, neg.Breakdown.SemanticRaw, 5);
    }
}
