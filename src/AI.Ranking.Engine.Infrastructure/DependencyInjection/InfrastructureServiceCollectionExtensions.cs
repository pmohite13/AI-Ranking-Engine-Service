using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Infrastructure.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Ranking.Engine.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers document parsers and <see cref="IDocumentParserFactory" />.
    /// </summary>
    public static IServiceCollection AddRankingEngineInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<PdfDocumentParser>();
        services.AddSingleton<DocxDocumentParser>();
        services.AddSingleton<PlainTextDocumentParser>();
        services.AddSingleton<IDocumentParserFactory, DocumentParserFactory>();

        return services;
    }
}
