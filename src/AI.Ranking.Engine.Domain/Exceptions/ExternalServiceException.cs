namespace AI.Ranking.Engine.Domain.Exceptions;

/// <summary>
/// Raised when an external dependency (e.g. OpenAI HTTP API) fails after retries or returns an unexpected result.
/// </summary>
public sealed class ExternalServiceException : Exception
{
    public ExternalServiceException(string message)
        : base(message)
    {
    }

    public ExternalServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
