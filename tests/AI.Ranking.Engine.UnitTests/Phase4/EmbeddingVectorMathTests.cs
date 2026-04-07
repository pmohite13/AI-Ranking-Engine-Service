using AI.Ranking.Engine.Domain.Exceptions;
using AI.Ranking.Engine.Infrastructure.VectorRecall;

namespace AI.Ranking.Engine.UnitTests.Phase4;

public sealed class EmbeddingVectorMathTests
{
    [Fact]
    public void DotProduct_orthogonal_unit_vectors_is_zero()
    {
        var a = new float[] { 1f, 0f, 0f };
        var b = new float[] { 0f, 1f, 0f };
        Assert.Equal(0f, EmbeddingVectorMath.DotProduct(a, b));
    }

    [Fact]
    public void DotProduct_same_direction_normalized_is_one()
    {
        var a = EmbeddingVectorMath.NormalizeCopy(new float[] { 3f, 4f, 0f });
        var b = EmbeddingVectorMath.NormalizeCopy(new float[] { 6f, 8f, 0f });
        Assert.InRange(EmbeddingVectorMath.DotProduct(a, b), 0.999f, 1.001f);
    }

    [Fact]
    public void DotProduct_mismatched_length_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            EmbeddingVectorMath.DotProduct(new float[] { 1f }, new float[] { 1f, 2f }));
    }

    [Fact]
    public void NormalizeCopy_empty_throws()
    {
        Assert.Throws<ArgumentException>(() => EmbeddingVectorMath.NormalizeCopy(ReadOnlySpan<float>.Empty));
    }

    [Fact]
    public void NormalizeCopy_zero_vector_throws_domain()
    {
        Assert.Throws<DomainException>(() => EmbeddingVectorMath.NormalizeCopy(new float[] { 0f, 0f }));
    }

    [Fact]
    public void L2Norm_unit_vector_is_one()
    {
        Assert.Equal(1f, EmbeddingVectorMath.L2Norm(new float[] { 1f, 0f, 0f }));
    }
}
