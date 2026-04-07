namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// One embedding vector returned from the provider. Values are typically L2-normalized by the client or index layer.
/// </summary>
public readonly record struct EmbeddingVector(float[] Values, int? EstimatedTokenCount);
