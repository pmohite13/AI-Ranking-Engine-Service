using AI.Ranking.Engine.Infrastructure.Embeddings;

namespace AI.Ranking.Engine.UnitTests.Phase3;

public sealed class EmbeddingTokenEstimatorTests
{
    [Fact]
    public void EstimateTokens_Empty_IsZero()
    {
        Assert.Equal(0, EmbeddingTokenEstimator.EstimateTokens(string.Empty, 4));
    }

    [Fact]
    public void EstimateTokens_ShortText_AtLeastOne()
    {
        Assert.Equal(1, EmbeddingTokenEstimator.EstimateTokens("a", 4));
    }

    [Fact]
    public void EstimateTokens_UsesCharsPerTokenRatio()
    {
        var text = new string('x', 40);
        Assert.Equal(10, EmbeddingTokenEstimator.EstimateTokens(text, 4));
    }
}
