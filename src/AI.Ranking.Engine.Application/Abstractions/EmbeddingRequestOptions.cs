namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// Embedding request parameters. Model id must participate in cache keys when implementations support caching.
/// </summary>
public sealed record EmbeddingRequestOptions(
    string ModelId,
    int Dimensions);
