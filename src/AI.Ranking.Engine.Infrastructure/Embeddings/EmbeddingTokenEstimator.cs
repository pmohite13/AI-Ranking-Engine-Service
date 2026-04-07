namespace AI.Ranking.Engine.Infrastructure.Embeddings;

/// <summary>
/// Fast token estimate for batching (not for billing). Uses character/byte heuristics.
/// </summary>
public static class EmbeddingTokenEstimator
{
    /// <summary>Estimates tokens from UTF-16 length using a configurable chars-per-token ratio.</summary>
    public static int EstimateTokens(string text, int estimatedCharsPerToken)
    {
        if (text.Length == 0)
            return 0;

        var ratio = estimatedCharsPerToken <= 0 ? 4 : estimatedCharsPerToken;
        return Math.Max(1, (text.Length + ratio - 1) / ratio);
    }
}
