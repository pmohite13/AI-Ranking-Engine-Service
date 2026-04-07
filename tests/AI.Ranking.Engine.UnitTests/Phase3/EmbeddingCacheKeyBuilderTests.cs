using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Infrastructure.Embeddings;

namespace AI.Ranking.Engine.UnitTests.Phase3;

public sealed class EmbeddingCacheKeyBuilderTests
{
    [Fact]
    public void Build_SameInputs_ProducesSameKey()
    {
        var opts = new EmbeddingRequestOptions("text-embedding-3-small", 1536);
        var a = EmbeddingCacheKeyBuilder.Build("hello world", opts);
        var b = EmbeddingCacheKeyBuilder.Build("hello world", opts);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Build_DifferentModel_ProducesDifferentKey()
    {
        var a = EmbeddingCacheKeyBuilder.Build("x", new EmbeddingRequestOptions("text-embedding-3-small", 1536));
        var b = EmbeddingCacheKeyBuilder.Build("x", new EmbeddingRequestOptions("text-embedding-3-large", 1536));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Build_DifferentDimensions_ProducesDifferentKey()
    {
        var a = EmbeddingCacheKeyBuilder.Build("x", new EmbeddingRequestOptions("text-embedding-3-small", 1536));
        var b = EmbeddingCacheKeyBuilder.Build("x", new EmbeddingRequestOptions("text-embedding-3-small", 512));
        Assert.NotEqual(a, b);
    }
}
