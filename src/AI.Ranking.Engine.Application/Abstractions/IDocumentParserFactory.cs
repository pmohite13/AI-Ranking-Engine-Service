using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// Resolves the <see cref="IDocumentParser" /> for a <see cref="DocumentContentType" /> (extension/MIME mapping lives at the call site).
/// </summary>
public interface IDocumentParserFactory
{
    /// <summary>Returns the parser for the content type, or throws if unsupported.</summary>
    IDocumentParser GetParser(DocumentContentType contentType);
}
