using System.Text;
using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Entities;
using AI.Ranking.Engine.Domain.Exceptions;
using AI.Ranking.Engine.Infrastructure.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AI.Ranking.Engine.Infrastructure.Parsing;

/// <summary>
/// DOCX text extraction via Open XML SDK (main document body).
/// </summary>
public sealed class DocxDocumentParser : IDocumentParser
{
    public bool CanParse(DocumentContentType contentType) => contentType == DocumentContentType.Docx;

    public async Task<ParsedDocument> ParseAsync(
        Stream stream,
        DocumentParseInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!CanParse(input.ContentType))
        {
            throw new ArgumentException("Content type must be DOCX.", nameof(input));
        }

        var (bytes, sha256) = await BoundedDocumentMaterializer.ReadAndHashAsync(
                stream,
                input.DeclaredContentLength,
                cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            using var doc = WordprocessingDocument.Open(ms, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                return new ParsedDocument
                {
                    NormalizedText = string.Empty,
                    ContentSha256 = sha256,
                    ContentType = DocumentContentType.Docx,
                };
            }

            var sb = new StringBuilder();
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                foreach (var text in paragraph.Descendants<Text>())
                {
                    sb.Append(text.Text);
                }

                sb.AppendLine();
            }

            var normalized = TextNormalizer.Normalize(sb.ToString());
            return new ParsedDocument
            {
                NormalizedText = normalized,
                ContentSha256 = sha256,
                ContentType = DocumentContentType.Docx,
            };
        }
        catch (Exception ex) when (ex is not DocumentParseException and not OperationCanceledException)
        {
            throw new DocumentParseException("Failed to parse DOCX content.", ex);
        }
    }
}
