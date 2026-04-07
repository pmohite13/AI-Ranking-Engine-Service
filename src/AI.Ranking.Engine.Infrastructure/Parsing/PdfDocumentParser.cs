using System.Text;
using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Entities;
using AI.Ranking.Engine.Domain.Exceptions;
using AI.Ranking.Engine.Infrastructure.IO;
using UglyToad.PdfPig;

namespace AI.Ranking.Engine.Infrastructure.Parsing;

/// <summary>
/// PDF text extraction via PdfPig (NuGet package id <c>PdfPig</c>).
/// </summary>
public sealed class PdfDocumentParser : IDocumentParser
{
    public bool CanParse(DocumentContentType contentType) => contentType == DocumentContentType.Pdf;

    public async Task<ParsedDocument> ParseAsync(
        Stream stream,
        DocumentParseInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!CanParse(input.ContentType))
        {
            throw new ArgumentException("Content type must be PDF.", nameof(input));
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
            using var pdf = PdfDocument.Open(ms);
            var sb = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }

            var normalized = TextNormalizer.Normalize(sb.ToString());
            return new ParsedDocument
            {
                NormalizedText = normalized,
                ContentSha256 = sha256,
                ContentType = DocumentContentType.Pdf,
            };
        }
        catch (Exception ex) when (ex is not DocumentParseException and not OperationCanceledException)
        {
            throw new DocumentParseException("Failed to parse PDF content.", ex);
        }
    }
}
