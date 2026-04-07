using AI.Ranking.Engine.Application.Abstractions;

namespace AI.Ranking.Engine.Infrastructure.VectorRecall;

/// <summary>
/// Selects Top-K by similarity with deterministic ordering: similarity descending, then candidate id ascending.
/// Uses a full sort for clarity and predictable behavior at moderate N (implementation plan target ~50k).
/// </summary>
internal static class VectorRecallTopK
{
    internal static List<VectorRecallMatch> SelectTopK(IReadOnlyList<VectorRecallMatch> all, int k)
    {
        if (k <= 0 || all.Count == 0)
            return new List<VectorRecallMatch>();

        var sorted = new List<VectorRecallMatch>(all);
        sorted.Sort(CompareBestFirst);

        var take = Math.Min(k, sorted.Count);
        return sorted.GetRange(0, take);
    }

    private static int CompareBestFirst(VectorRecallMatch a, VectorRecallMatch b)
    {
        var c = b.Similarity.CompareTo(a.Similarity);
        if (c != 0)
            return c;

        return string.CompareOrdinal(a.CandidateId, b.CandidateId);
    }
}
