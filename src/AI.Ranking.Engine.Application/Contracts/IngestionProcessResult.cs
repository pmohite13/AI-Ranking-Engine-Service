namespace AI.Ranking.Engine.Application.Contracts;

public sealed record IngestionProcessResult(
    string EntityId,
    IngestionEntityKind EntityKind,
    string FileName,
    string ContentType,
    int EmbeddingDimensions,
    int SkillCount,
    bool Deduplicated);
