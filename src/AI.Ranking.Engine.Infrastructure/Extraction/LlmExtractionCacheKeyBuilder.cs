using System.Security.Cryptography;
using System.Text;

namespace AI.Ranking.Engine.Infrastructure.Extraction;

internal static class LlmExtractionCacheKeyBuilder
{
    private const string Prefix = "llm-extract:v1:";

    public static string Build(string modelId, ExtractionDocumentKind kind, string normalizedText)
    {
        ArgumentNullException.ThrowIfNull(modelId);
        ArgumentNullException.ThrowIfNull(normalizedText);

        var payload = string.Concat(modelId.Trim(), "\u001F", kind.ToString(), "\u001F", normalizedText);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Prefix + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
