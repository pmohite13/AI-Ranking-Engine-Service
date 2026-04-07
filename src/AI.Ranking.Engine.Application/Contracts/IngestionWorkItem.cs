using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.Application.Contracts;

public sealed record IngestionWorkItem(
    string EntityId,
    IngestionEntityKind EntityKind,
    string FileName,
    DocumentContentType ContentType,
    byte[] FileBytes);
