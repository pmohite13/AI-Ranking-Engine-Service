using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.Application.Contracts;

/// <summary>
/// API contract for document ingestion validation (size, type, identity). Binary payload is handled at the HTTP layer.
/// </summary>
public sealed record IngestRequest(
    string EntityId,
    string FileName,
    long ContentLength,
    DocumentContentType ContentType);
