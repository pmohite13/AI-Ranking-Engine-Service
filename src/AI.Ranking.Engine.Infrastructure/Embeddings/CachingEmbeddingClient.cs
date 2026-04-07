using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Infrastructure.Caching;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.Infrastructure.Embeddings;

/// <summary>
/// Decorator: caches each text embedding by SHA-256 key (canonical text + model + dimensions). Misses are batched via the inner client.
/// </summary>
public sealed class CachingEmbeddingClient : IEmbeddingClient
{
    private readonly IEmbeddingClient _inner;
    private readonly ICacheService _cache;
    private readonly IOptions<EmbeddingOptions> _options;

    public CachingEmbeddingClient(
        IEmbeddingClient inner,
        ICacheService cache,
        IOptions<EmbeddingOptions> options)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<IReadOnlyList<EmbeddingVector>> EmbedAsync(
        IReadOnlyList<string> texts,
        EmbeddingRequestOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);
        ArgumentNullException.ThrowIfNull(options);

        if (texts.Count == 0)
            return Array.Empty<EmbeddingVector>();

        var expiration = _options.Value.EmbeddingCacheExpiration;
        var results = new EmbeddingVector[texts.Count];
        var missIndices = new List<int>();
        var missTexts = new List<string>();

        for (var i = 0; i < texts.Count; i++)
        {
            var key = EmbeddingCacheKeyBuilder.Build(texts[i], options);
            var cached = await _cache.GetAsync<CachedEmbeddingPayload>(key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                results[i] = new EmbeddingVector(ToFloatCopy(cached.Values), cached.EstimatedTokenCount);
            }
            else
            {
                missIndices.Add(i);
                missTexts.Add(texts[i]);
            }
        }

        if (missTexts.Count == 0)
            return results;

        var embedded = await _inner.EmbedAsync(missTexts, options, cancellationToken).ConfigureAwait(false);

        for (var j = 0; j < missIndices.Count; j++)
        {
            var idx = missIndices[j];
            var ev = embedded[j];
            results[idx] = ev;

            var key = EmbeddingCacheKeyBuilder.Build(texts[idx], options);
            await _cache.SetAsync(
                    key,
                    new CachedEmbeddingPayload
                    {
                        Values = ToFloatCopy(ev.Values),
                        EstimatedTokenCount = ev.EstimatedTokenCount,
                    },
                    expiration,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return results;
    }

    private static float[] ToFloatCopy(float[] values)
    {
        var copy = new float[values.Length];
        Array.Copy(values, copy, values.Length);
        return copy;
    }
}
