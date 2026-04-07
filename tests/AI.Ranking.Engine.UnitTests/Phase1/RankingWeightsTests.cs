using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Exceptions;

namespace AI.Ranking.Engine.UnitTests.Phase1;

public sealed class RankingWeightsTests
{
    [Fact]
    public void Default_weights_sum_to_one_and_pass_validation()
    {
        var w = RankingWeights.Default;
        w.EnsureValid();
        Assert.Equal(1.0, w.Semantic + w.SkillOverlap + w.ExperienceFit + w.Keyword, 6);
    }

    [Fact]
    public void EnsureValid_throws_when_sum_not_one()
    {
        var w = new RankingWeights(0.5, 0.5, 0.5, 0.0);
        var ex = Assert.Throws<InvalidRankingConfigurationException>(() => w.EnsureValid());
        Assert.Contains("sum", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureValid_throws_on_negative_component()
    {
        var w = new RankingWeights(1.0, 0, 0, 0);
        w.EnsureValid();

        var bad = new RankingWeights(-0.1, 0.6, 0.3, 0.2);
        Assert.Throws<InvalidRankingConfigurationException>(() => bad.EnsureValid());
    }
}
