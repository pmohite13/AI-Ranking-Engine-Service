using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Infrastructure.Parsing;

namespace AI.Ranking.Engine.UnitTests.Phase2;

public sealed class DocumentParserFactoryTests
{
    private readonly DocumentParserFactory _factory = new(
        new PdfDocumentParser(),
        new DocxDocumentParser(),
        new PlainTextDocumentParser());

    [Theory]
    [InlineData(DocumentContentType.Pdf, typeof(PdfDocumentParser))]
    [InlineData(DocumentContentType.Docx, typeof(DocxDocumentParser))]
    [InlineData(DocumentContentType.PlainText, typeof(PlainTextDocumentParser))]
    public void GetParser_ReturnsExpectedImplementation(DocumentContentType type, Type expectedType)
    {
        var parser = _factory.GetParser(type);
        Assert.IsType(expectedType, parser);
    }
}
