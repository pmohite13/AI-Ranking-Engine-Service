using AI.Ranking.Engine.Application.Contracts;

namespace AI.Ranking.Engine.Application.Abstractions;

public interface IIngestionQueue
{
    int ApproximateDepth { get; }

    ValueTask<IngestionEnqueueResult> EnqueueAsync(IngestionWorkItem workItem, CancellationToken cancellationToken = default);
}
