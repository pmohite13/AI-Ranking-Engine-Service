namespace AI.Ranking.Engine.Infrastructure.Caching;

/// <summary>
/// Serializable embedding payload stored in <see cref="Application.Abstractions.ICacheService" />.
/// </summary>
public sealed class CachedEmbeddingPayload
{
    public required float[] Values { get; init; }

    public int? EstimatedTokenCount { get; init; }
}
