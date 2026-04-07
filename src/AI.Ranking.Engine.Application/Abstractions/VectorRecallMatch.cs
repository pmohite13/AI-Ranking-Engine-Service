namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// One hit from vector recall (cosine/dot on normalized vectors).
/// </summary>
public sealed record VectorRecallMatch(string CandidateId, float Similarity);
