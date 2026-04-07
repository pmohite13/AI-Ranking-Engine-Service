using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Infrastructure.Caching;
using AI.Ranking.Engine.Infrastructure.Embeddings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.UnitTests.Phase3;

public sealed class CachingEmbeddingClientTests
{
    private static (CachingEmbeddingClient Client, FakeEmbeddingClient Inner) CreateSut()
    {
        var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        var cacheService = new MemoryCacheService(memoryCache);
        var inner = new FakeEmbeddingClient();
        var embeddingOptions = Options.Create(new EmbeddingOptions());
        var sut = new CachingEmbeddingClient(inner, cacheService, embeddingOptions);
        return (sut, inner);
    }

    [Fact]
    public async Task EmbedAsync_SecondCallWithSameTexts_DoesNotCallInnerAgain()
    {
        var (sut, inner) = CreateSut();
        var opts = new EmbeddingRequestOptions("text-embedding-3-small", 1536);
        var texts = new[] { "alpha", "beta" };

        await sut.EmbedAsync(texts, opts, CancellationToken.None);
        Assert.Equal(1, inner.CallCount);

        await sut.EmbedAsync(texts, opts, CancellationToken.None);
        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task EmbedAsync_DifferentModel_BypassesCacheForNewModel()
    {
        var (sut, inner) = CreateSut();
        var texts = new[] { "same" };

        await sut.EmbedAsync(texts, new EmbeddingRequestOptions("m1", 1536), CancellationToken.None);
        await sut.EmbedAsync(texts, new EmbeddingRequestOptions("m2", 1536), CancellationToken.None);

        Assert.Equal(2, inner.CallCount);
    }

    private sealed class FakeEmbeddingClient : IEmbeddingClient
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<EmbeddingVector>> EmbedAsync(
            IReadOnlyList<string> texts,
            EmbeddingRequestOptions options,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            IReadOnlyList<EmbeddingVector> result = texts
                .Select((_, i) => new EmbeddingVector(new[] { i + 0.1f, i + 0.2f }, 1))
                .ToList();
            return Task.FromResult(result);
        }
    }
}
