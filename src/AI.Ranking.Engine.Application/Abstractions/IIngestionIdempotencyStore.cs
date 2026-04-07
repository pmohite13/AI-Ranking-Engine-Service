using AI.Ranking.Engine.Application.Contracts;

namespace AI.Ranking.Engine.Application.Abstractions;

public interface IIngestionIdempotencyStore
{
    bool TryStart(string key, out Task<IngestionProcessResult>? existingTask);

    void Complete(string key, IngestionProcessResult result);

    void Fail(string key, Exception exception);
}
