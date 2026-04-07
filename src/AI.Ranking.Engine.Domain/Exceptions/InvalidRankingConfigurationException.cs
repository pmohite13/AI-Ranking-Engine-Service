namespace AI.Ranking.Engine.Domain.Exceptions;

public sealed class InvalidRankingConfigurationException : DomainException
{
    public InvalidRankingConfigurationException(string message)
        : base(message)
    {
    }
}
