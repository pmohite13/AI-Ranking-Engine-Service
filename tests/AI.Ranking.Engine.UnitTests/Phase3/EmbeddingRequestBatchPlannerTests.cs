using AI.Ranking.Engine.Domain.Exceptions;
using AI.Ranking.Engine.Infrastructure.Embeddings;

namespace AI.Ranking.Engine.UnitTests.Phase3;

public sealed class EmbeddingRequestBatchPlannerTests
{
    [Fact]
    public void Plan_Empty_ReturnsEmpty()
    {
        var batches = EmbeddingRequestBatchPlanner.Plan(
            Array.Empty<string>(),
            maxInputsPerHttpRequest: 10,
            maxTokensPerInput: 100,
            maxEstimatedTokensPerHttpRequest: 1000,
            estimatedCharsPerToken: 4);
        Assert.Empty(batches);
    }

    [Fact]
    public void Plan_SplitsOnMaxInputs()
    {
        var texts = new[] { "a", "b", "c" };
        var batches = EmbeddingRequestBatchPlanner.Plan(
            texts,
            maxInputsPerHttpRequest: 2,
            maxTokensPerInput: 1000,
            maxEstimatedTokensPerHttpRequest: 100_000,
            estimatedCharsPerToken: 4);
        Assert.Equal(2, batches.Count);
        Assert.Equal(2, batches[0].Count);
        Assert.Single(batches[1]);
    }

    [Fact]
    public void Plan_InputExceedingTokenCeiling_ThrowsExternalServiceException()
    {
        var longText = new string('x', 50_000);
        var ex = Assert.Throws<ExternalServiceException>(() => EmbeddingRequestBatchPlanner.Plan(
            new[] { longText },
            maxInputsPerHttpRequest: 10,
            maxTokensPerInput: 100,
            maxEstimatedTokensPerHttpRequest: 1_000_000,
            estimatedCharsPerToken: 4));
        Assert.Contains("token ceiling", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
