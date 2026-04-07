using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Entities;

namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// Format-specific document parser. Implementations live in infrastructure (PdfPig, Open XML, plain text).
/// </summary>
public interface IDocumentParser
{
    bool CanParse(DocumentContentType contentType);

    Task<ParsedDocument> ParseAsync(Stream stream, DocumentParseInput input, CancellationToken cancellationToken = default);
}
