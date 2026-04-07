namespace AI.Ranking.Engine.Domain.Exceptions;

/// <summary>
/// Base type for invalid domain state (not request validation or external dependency failures).
/// </summary>
public class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
