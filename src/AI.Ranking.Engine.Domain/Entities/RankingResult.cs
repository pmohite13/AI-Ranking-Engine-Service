using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.Domain.Entities;

/// <summary>
/// One row in a ranked result list (Top-N per job).
/// </summary>
public sealed class RankingResult
{
    public required string CandidateId { get; init; }

    public required int Rank { get; init; }

    public required double TotalScore { get; init; }

    public required ScoreBreakdown Breakdown { get; init; }
}
