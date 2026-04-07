using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Exceptions;
using AI.Ranking.Engine.Infrastructure.Parsing;

namespace AI.Ranking.Engine.UnitTests.Phase2;

public sealed class PdfDocumentParserTests
{
    private readonly PdfDocumentParser _parser = new();

    [Fact]
    public async Task ParseAsync_FixturePdf_ContainsExpectedText()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "hello.pdf");
        Assert.True(File.Exists(path), $"Missing fixture: {path}");

        await using var stream = File.OpenRead(path);
        var length = stream.Length;
        var input = new DocumentParseInput("hello.pdf", DocumentContentType.Pdf, length);

        var result = await _parser.ParseAsync(stream, input, CancellationToken.None);

        Assert.Equal(DocumentContentType.Pdf, result.ContentType);
        Assert.Equal(32, result.ContentSha256.Length);
        Assert.Contains("Hello Fixture PDF", result.NormalizedText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParseAsync_InvalidPdf_ThrowsDocumentParseException()
    {
        var garbage = "not a pdf"u8.ToArray();
        await using var stream = new MemoryStream(garbage);
        var input = new DocumentParseInput("bad.pdf", DocumentContentType.Pdf, garbage.Length);

        await Assert.ThrowsAsync<DocumentParseException>(() =>
            _parser.ParseAsync(stream, input, CancellationToken.None));
    }
}
