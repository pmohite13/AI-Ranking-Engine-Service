namespace AI.Ranking.Engine.Application.Options;

/// <summary>
/// Configurable ingestion limits. Defaults align with implementation plan (200 KB cap).
/// </summary>
public sealed class IngestOptions
{
    public const string SectionName = "Ingest";

    /// <summary>Maximum upload size in bytes (default 200 KiB).</summary>
    public int MaxUploadBytes { get; set; } = 200 * 1024;
}
