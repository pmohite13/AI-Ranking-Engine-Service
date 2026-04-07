namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// Embedding provider (e.g. OpenAI). Implementations batch in-process per implementation plan Part E.3.
/// </summary>
public interface IEmbeddingClient
{
    Task<IReadOnlyList<EmbeddingVector>> EmbedAsync(
        IReadOnlyList<string> texts,
        EmbeddingRequestOptions options,
        CancellationToken cancellationToken = default);
}
