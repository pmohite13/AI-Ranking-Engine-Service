using AI.Ranking.Engine.Application.Contracts;

namespace AI.Ranking.Engine.Application.Abstractions;

public interface IDocumentIngestionPipeline
{
    Task<IngestionProcessResult> ProcessAsync(IngestionWorkItem workItem, CancellationToken cancellationToken = default);
}
