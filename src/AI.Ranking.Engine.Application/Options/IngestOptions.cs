namespace AI.Ranking.Engine.Application.Options;

/// <summary>
/// Configurable ingestion limits. Defaults align with implementation plan (200 KB cap).
/// </summary>
public sealed class IngestOptions
{
    public const string SectionName = "Ingest";

    /// <summary>Maximum upload size in bytes (default 200 KiB).</summary>
    public int MaxUploadBytes { get; set; } = 200 * 1024;

    /// <summary>
    /// How long successful ingestion dedupe metadata is retained in the shared memory cache (same entity + same bytes + same embedding model).
    /// Keeps sequential duplicate uploads cheap without growing an in-memory dictionary forever.
    /// </summary>
    public int IngestionDedupCacheHours { get; set; } = 24;
}
