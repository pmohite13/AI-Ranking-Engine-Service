using System.Text;
using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Entities;
using AI.Ranking.Engine.Infrastructure.IO;

namespace AI.Ranking.Engine.Infrastructure.Parsing;

/// <summary>
/// UTF-8 plain text (for tests and text-only pipelines; no binary format handling).
/// </summary>
public sealed class PlainTextDocumentParser : IDocumentParser
{
    public bool CanParse(DocumentContentType contentType) => contentType == DocumentContentType.PlainText;

    public async Task<ParsedDocument> ParseAsync(
        Stream stream,
        DocumentParseInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!CanParse(input.ContentType))
        {
            throw new ArgumentException("Content type must be PlainText.", nameof(input));
        }

        var (bytes, sha256) = await BoundedDocumentMaterializer.ReadAndHashAsync(
                stream,
                input.DeclaredContentLength,
                cancellationToken)
            .ConfigureAwait(false);

        var raw = Encoding.UTF8.GetString(bytes);
        var normalized = TextNormalizer.Normalize(raw);
        return new ParsedDocument
        {
            NormalizedText = normalized,
            ContentSha256 = sha256,
            ContentType = DocumentContentType.PlainText,
        };
    }
}
