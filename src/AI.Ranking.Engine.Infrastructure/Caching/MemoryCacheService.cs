using AI.Ranking.Engine.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace AI.Ranking.Engine.Infrastructure.Caching;

/// <summary>
/// Hash-keyed <see cref="IMemoryCache" /> adapter for embedding and future parse/LLM caches.
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    public ValueTask<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        _memoryCache.TryGetValue(cacheKey, out T? value);
        return ValueTask.FromResult(value);
    }

    public Task SetAsync<T>(
        string cacheKey,
        T value,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = new MemoryCacheEntryOptions();
        if (absoluteExpirationRelativeToNow is { } exp)
            options.SetAbsoluteExpiration(exp);

        _memoryCache.Set(cacheKey, value, options);
        return Task.CompletedTask;
    }
}
