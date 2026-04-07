namespace AI.Ranking.Engine.Domain;

/// <summary>
/// Supported document formats for ingestion. Additional formats extend this enum and register parsers in infrastructure.
/// </summary>
public enum DocumentContentType
{
    Pdf = 0,
    Docx = 1,
    /// <summary>Used for tests and plain-text pipelines without binary parsing.</summary>
    PlainText = 2,
}
