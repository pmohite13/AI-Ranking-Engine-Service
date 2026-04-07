using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.Domain.Entities;

/// <summary>
/// Output of format-specific parsing: normalized text plus integrity metadata for caching.
/// </summary>
public sealed class ParsedDocument
{
    public required string NormalizedText { get; init; }

    /// <summary>SHA-256 over canonical input bytes (or normalized text policy defined by ingestion).</summary>
    public required byte[] ContentSha256 { get; init; }

    public required DocumentContentType ContentType { get; init; }
}
