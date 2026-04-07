using AI.Ranking.Engine.Application.Contracts;

namespace AI.Ranking.Engine.Infrastructure.Ingestion;

public sealed class IngestionQueueMessage
{
    public required IngestionWorkItem WorkItem { get; init; }

    public required TaskCompletionSource<IngestionProcessResult> CompletionSource { get; init; }
}
