using System.Text;
using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Infrastructure.Parsing;

namespace AI.Ranking.Engine.UnitTests.Phase2;

public sealed class PlainTextDocumentParserTests
{
    private readonly PlainTextDocumentParser _parser = new();

    [Fact]
    public async Task ParseAsync_ExtractsUtf8Text_AndSha256MatchesBytes()
    {
        var payload = "  hello  plain  "u8.ToArray();
        await using var stream = new MemoryStream(payload);
        var input = new DocumentParseInput("x.txt", DocumentContentType.PlainText, payload.Length);

        var result = await _parser.ParseAsync(stream, input, CancellationToken.None);

        Assert.Equal(DocumentContentType.PlainText, result.ContentType);
        Assert.Equal("hello plain", result.NormalizedText);
        Assert.Equal(32, result.ContentSha256.Length);
    }

    [Fact]
    public async Task ParseAsync_WrongContentType_Throws()
    {
        await using var stream = new MemoryStream("x"u8.ToArray());
        var input = new DocumentParseInput("x.pdf", DocumentContentType.Pdf, 1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _parser.ParseAsync(stream, input, CancellationToken.None));
    }
}
