using System.Security.Cryptography;
using System.Text;
using AI.Ranking.Engine.Application.Abstractions;

namespace AI.Ranking.Engine.Infrastructure.Embeddings;

/// <summary>
/// SHA-256 cache keys over canonical text + model + dimensions (implementation plan Part E.5).
/// </summary>
public static class EmbeddingCacheKeyBuilder
{
    private const string Prefix = "emb:v1:";

    public static string Build(string canonicalText, EmbeddingRequestOptions options)
    {
        ArgumentNullException.ThrowIfNull(canonicalText);
        ArgumentNullException.ThrowIfNull(options);

        var model = options.ModelId.Trim();
        var dim = options.Dimensions.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Unit separator avoids ambiguous concatenation of adjacent fields.
        var payload = string.Concat(canonicalText, "\u001F", model, "\u001F", dim);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Prefix + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
