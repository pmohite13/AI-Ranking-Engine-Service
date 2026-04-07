namespace AI.Ranking.Engine.Api.Contracts;

public enum IngestionEntityKind
{
    Candidate = 0,
    Job = 1,
}

public sealed class DocumentIngestForm
{
    public string EntityId { get; init; } = string.Empty;

    public IngestionEntityKind EntityKind { get; init; }

    public IFormFile? File { get; init; }
}

public sealed record RankJobRequest(int VectorRecallTopK = 50, int FinalTopN = 10);

public sealed record IngestDocumentResponse(
    string EntityId,
    IngestionEntityKind EntityKind,
    string FileName,
    string ContentType,
    int EmbeddingDimensions,
    int SkillCount,
    bool Deduplicated,
    int QueueDepthAtEnqueue);

public sealed record RankJobResponse(
    string JobId,
    int VectorRecallTopK,
    int FinalTopN,
    IReadOnlyList<RankedCandidateResponse> Results);

public sealed record RankedCandidateResponse(
    string CandidateId,
    int Rank,
    double TotalScore,
    object Breakdown);
