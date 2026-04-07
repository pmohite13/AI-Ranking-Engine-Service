using System.Numerics;
using AI.Ranking.Engine.Domain.Exceptions;

namespace AI.Ranking.Engine.Infrastructure.VectorRecall;

/// <summary>
/// Pure managed embedding math: L2 normalization and dot product for cosine similarity on normalized vectors.
/// </summary>
public static class EmbeddingVectorMath
{
    /// <summary>Computes dot product of two equal-length vectors.</summary>
    public static float DotProduct(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have the same dimension.");

        if (a.Length == 0)
            return 0f;

        if (Vector.IsHardwareAccelerated && a.Length >= Vector<float>.Count)
            return DotProductSimd(a, b);

        var sum = 0f;
        for (var i = 0; i < a.Length; i++)
            sum += a[i] * b[i];

        return sum;
    }

    /// <summary>
    /// Returns an L2-normalized copy. Throws if the vector is empty, all zeros, or non-finite.
    /// </summary>
    public static float[] NormalizeCopy(ReadOnlySpan<float> source)
    {
        if (source.IsEmpty)
            throw new ArgumentException("Embedding vector cannot be empty.", nameof(source));

        var copy = new float[source.Length];
        source.CopyTo(copy);
        var norm = L2Norm(copy);
        if (norm <= 0f || float.IsNaN(norm) || float.IsInfinity(norm))
            throw new DomainException("Embedding vector has zero or invalid L2 norm.");

        var inv = 1f / norm;
        for (var i = 0; i < copy.Length; i++)
            copy[i] *= inv;

        return copy;
    }

    public static float L2Norm(ReadOnlySpan<float> values)
    {
        if (values.IsEmpty)
            return 0f;

        if (Vector.IsHardwareAccelerated && values.Length >= Vector<float>.Count)
            return MathF.Sqrt(SumSquaresSimd(values));

        var sum = 0d;
        for (var i = 0; i < values.Length; i++)
        {
            var v = values[i];
            sum += (double)v * v;
        }

        return MathF.Sqrt((float)sum);
    }

    private static float DotProductSimd(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        var acc = Vector<float>.Zero;
        var i = 0;
        for (; i <= a.Length - Vector<float>.Count; i += Vector<float>.Count)
        {
            var va = new Vector<float>(a.Slice(i));
            var vb = new Vector<float>(b.Slice(i));
            acc += va * vb;
        }

        var sum = Vector.Sum(acc);
        for (; i < a.Length; i++)
            sum += a[i] * b[i];

        return sum;
    }

    private static float SumSquaresSimd(ReadOnlySpan<float> values)
    {
        var acc = Vector<float>.Zero;
        var i = 0;
        for (; i <= values.Length - Vector<float>.Count; i += Vector<float>.Count)
        {
            var v = new Vector<float>(values.Slice(i));
            acc += v * v;
        }

        var sum = (double)Vector.Sum(acc);
        for (; i < values.Length; i++)
        {
            var v = values[i];
            sum += (double)v * v;
        }

        return (float)sum;
    }
}
