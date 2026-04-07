using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// Metadata passed to parsers; keeps streams as parameters on <see cref="IDocumentParser.ParseAsync" />.
/// </summary>
public sealed record DocumentParseInput(
    string FileName,
    DocumentContentType ContentType,
    long DeclaredContentLength);
