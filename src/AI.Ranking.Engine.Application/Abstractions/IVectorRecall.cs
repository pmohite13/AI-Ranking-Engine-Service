namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// Pure .NET in-memory recall: add/update candidate vectors and retrieve Top-K by similarity to a query vector.
/// </summary>
public interface IVectorRecall
{
    Task UpsertCandidateAsync(string candidateId, float[] embedding, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorRecallMatch>> SearchTopKAsync(
        float[] queryEmbedding,
        int k,
        CancellationToken cancellationToken = default);
}
