using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Infrastructure.Parsing;

namespace AI.Ranking.Engine.UnitTests.Phase2;

public sealed class DocxDocumentParserTests
{
    private readonly DocxDocumentParser _parser = new();

    [Fact]
    public async Task ParseAsync_FixtureDocx_ContainsExpectedText()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "hello.docx");
        Assert.True(File.Exists(path), $"Missing fixture: {path}");

        await using var stream = File.OpenRead(path);
        var length = stream.Length;
        var input = new DocumentParseInput("hello.docx", DocumentContentType.Docx, length);

        var result = await _parser.ParseAsync(stream, input, CancellationToken.None);

        Assert.Equal(DocumentContentType.Docx, result.ContentType);
        Assert.Equal(32, result.ContentSha256.Length);
        Assert.Contains("Hello Fixture DOCX", result.NormalizedText, StringComparison.OrdinalIgnoreCase);
    }
}
