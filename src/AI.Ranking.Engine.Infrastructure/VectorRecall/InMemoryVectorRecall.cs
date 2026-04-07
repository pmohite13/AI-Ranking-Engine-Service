using System.Collections.Concurrent;
using AI.Ranking.Engine.Application.Abstractions;

namespace AI.Ranking.Engine.Infrastructure.VectorRecall;

/// <summary>
/// Thread-safe in-memory vector store: L2-normalized embeddings and Top-K cosine recall via dot product.
/// </summary>
public sealed class InMemoryVectorRecall : IVectorRecall
{
    private readonly ConcurrentDictionary<string, float[]> _vectors = new(StringComparer.Ordinal);
    private readonly object _dimensionLock = new();
    private int _expectedDimension = -1;

    public Task UpsertCandidateAsync(string candidateId, float[] embedding, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embedding);
        if (string.IsNullOrWhiteSpace(candidateId))
            throw new ArgumentException("Candidate id is required.", nameof(candidateId));

        cancellationToken.ThrowIfCancellationRequested();

        var normalized = EmbeddingVectorMath.NormalizeCopy(embedding);

        lock (_dimensionLock)
        {
            if (_expectedDimension < 0)
                _expectedDimension = normalized.Length;
            else if (_expectedDimension != normalized.Length)
            {
                throw new ArgumentException(
                    $"Embedding dimension {normalized.Length} does not match index dimension {_expectedDimension}.",
                    nameof(embedding));
            }
        }

        _vectors[candidateId] = normalized;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorRecallMatch>> SearchTopKAsync(
        float[] queryEmbedding,
        int k,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);
        cancellationToken.ThrowIfCancellationRequested();

        if (k <= 0)
            return Task.FromResult<IReadOnlyList<VectorRecallMatch>>(Array.Empty<VectorRecallMatch>());

        if (_vectors.IsEmpty)
            return Task.FromResult<IReadOnlyList<VectorRecallMatch>>(Array.Empty<VectorRecallMatch>());

        int dim;
        lock (_dimensionLock)
            dim = _expectedDimension;

        if (dim < 0)
            return Task.FromResult<IReadOnlyList<VectorRecallMatch>>(Array.Empty<VectorRecallMatch>());

        if (queryEmbedding.Length != dim)
        {
            throw new ArgumentException(
                $"Query embedding dimension {queryEmbedding.Length} does not match index dimension {dim}.",
                nameof(queryEmbedding));
        }

        var query = EmbeddingVectorMath.NormalizeCopy(queryEmbedding);

        var buffer = new List<VectorRecallMatch>(_vectors.Count);
        foreach (var pair in _vectors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sim = EmbeddingVectorMath.DotProduct(query, pair.Value);
            buffer.Add(new VectorRecallMatch(pair.Key, sim));
        }

        var top = VectorRecallTopK.SelectTopK(buffer, k);
        return Task.FromResult<IReadOnlyList<VectorRecallMatch>>(top);
    }
}
