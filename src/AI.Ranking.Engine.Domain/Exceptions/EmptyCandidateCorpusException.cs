namespace AI.Ranking.Engine.Domain.Exceptions;

/// <summary>
/// Thrown when ranking or recall is requested but no candidates are available (e.g. cold index).
/// </summary>
public sealed class EmptyCandidateCorpusException : DomainException
{
    public EmptyCandidateCorpusException(string message)
        : base(message)
    {
    }
}
