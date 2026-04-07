namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// Cache abstraction for embeddings, parses, and LLM outputs. Keys should incorporate model/version and canonical input hashes.
/// </summary>
public interface ICacheService
{
    ValueTask<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class;

    Task SetAsync<T>(
        string cacheKey,
        T value,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        CancellationToken cancellationToken = default)
        where T : class;
}
