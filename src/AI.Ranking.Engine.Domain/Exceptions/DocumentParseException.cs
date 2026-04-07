namespace AI.Ranking.Engine.Domain.Exceptions;

/// <summary>
/// Thrown when binary content cannot be parsed as the declared format (corrupt PDF/DOCX, invalid encoding, etc.).
/// </summary>
public sealed class DocumentParseException : DomainException
{
    public DocumentParseException(string message)
        : base(message)
    {
    }

    public DocumentParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
