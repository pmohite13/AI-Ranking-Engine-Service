using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.Infrastructure.Parsing;

/// <summary>
/// Selects the parser registered for each <see cref="DocumentContentType" />.
/// </summary>
public sealed class DocumentParserFactory : IDocumentParserFactory
{
    private readonly IReadOnlyDictionary<DocumentContentType, IDocumentParser> _parsers;

    public DocumentParserFactory(
        PdfDocumentParser pdfParser,
        DocxDocumentParser docxParser,
        PlainTextDocumentParser plainTextParser)
    {
        ArgumentNullException.ThrowIfNull(pdfParser);
        ArgumentNullException.ThrowIfNull(docxParser);
        ArgumentNullException.ThrowIfNull(plainTextParser);

        _parsers = new Dictionary<DocumentContentType, IDocumentParser>
        {
            [DocumentContentType.Pdf] = pdfParser,
            [DocumentContentType.Docx] = docxParser,
            [DocumentContentType.PlainText] = plainTextParser,
        };
    }

    public IDocumentParser GetParser(DocumentContentType contentType)
    {
        if (!_parsers.TryGetValue(contentType, out var parser))
        {
            throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "No parser registered for this content type.");
        }

        return parser;
    }
}
