using System.Collections.Concurrent;
using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Application.Contracts;

namespace AI.Ranking.Engine.Infrastructure.Ingestion;

public sealed class InMemoryIngestionIdempotencyStore : IIngestionIdempotencyStore
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<IngestionProcessResult>> _entries = new(StringComparer.Ordinal);

    public bool TryStart(string key, out Task<IngestionProcessResult>? existingTask)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var tcs = new TaskCompletionSource<IngestionProcessResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (_entries.TryAdd(key, tcs))
        {
            existingTask = null;
            return true;
        }

        existingTask = _entries[key].Task;
        return false;
    }

    public void Complete(string key, IngestionProcessResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_entries.TryRemove(key, out var tcs))
        {
            tcs.TrySetResult(result);
        }
    }

    public void Fail(string key, Exception exception)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(exception);

        if (_entries.TryRemove(key, out var tcs))
        {
            tcs.TrySetException(exception);
        }
    }
}
