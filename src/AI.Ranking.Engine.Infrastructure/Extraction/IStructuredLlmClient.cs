namespace AI.Ranking.Engine.Infrastructure.Extraction;

public interface IStructuredLlmClient
{
    Task<StructuredExtractionPayload?> ExtractAsync(
        string normalizedText,
        ExtractionDocumentKind kind,
        CancellationToken cancellationToken = default);
}
