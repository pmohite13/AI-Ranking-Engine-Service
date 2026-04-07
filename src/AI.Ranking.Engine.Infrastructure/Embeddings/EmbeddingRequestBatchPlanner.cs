using AI.Ranking.Engine.Domain.Exceptions;

namespace AI.Ranking.Engine.Infrastructure.Embeddings;

/// <summary>
/// Splits embedding inputs into in-process HTTP batches respecting token and count limits (sync batching only; no OpenAI Batch API).
/// </summary>
public static class EmbeddingRequestBatchPlanner
{
    public static IReadOnlyList<IReadOnlyList<string>> Plan(
        IReadOnlyList<string> texts,
        int maxInputsPerHttpRequest,
        int maxTokensPerInput,
        int maxEstimatedTokensPerHttpRequest,
        int estimatedCharsPerToken)
    {
        ArgumentNullException.ThrowIfNull(texts);
        if (maxInputsPerHttpRequest <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxInputsPerHttpRequest));
        if (maxTokensPerInput <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTokensPerInput));
        if (maxEstimatedTokensPerHttpRequest <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxEstimatedTokensPerHttpRequest));

        if (texts.Count == 0)
            return Array.Empty<IReadOnlyList<string>>();

        var batches = new List<List<string>>();
        var current = new List<string>();
        var batchTokens = 0;

        foreach (var text in texts)
        {
            var t = EmbeddingTokenEstimator.EstimateTokens(text, estimatedCharsPerToken);
            if (t > maxTokensPerInput)
            {
                throw new ExternalServiceException(
                    $"Embedding input exceeds configured token ceiling ({maxTokensPerInput}). Shorten or chunk the document upstream.");
            }

            var wouldExceedInputs = current.Count >= maxInputsPerHttpRequest;
            var wouldExceedTokens = current.Count > 0 && batchTokens + t > maxEstimatedTokensPerHttpRequest;

            if (wouldExceedInputs || wouldExceedTokens)
            {
                batches.Add(current);
                current = new List<string>();
                batchTokens = 0;
            }

            current.Add(text);
            batchTokens += t;
        }

        if (current.Count > 0)
            batches.Add(current);

        return batches;
    }
}
