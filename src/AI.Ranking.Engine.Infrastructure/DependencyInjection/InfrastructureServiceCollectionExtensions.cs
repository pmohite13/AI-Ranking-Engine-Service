using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Infrastructure.Caching;
using AI.Ranking.Engine.Infrastructure.Embeddings;
using AI.Ranking.Engine.Infrastructure.Http;
using AI.Ranking.Engine.Infrastructure.Parsing;
using AI.Ranking.Engine.Infrastructure.VectorRecall;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers document parsers, embeddings (OpenAI + cache decorator), memory cache, in-memory vector recall, and HTTP resilience.
    /// </summary>
    public static IServiceCollection AddRankingEngineInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<EmbeddingOptions>(configuration.GetSection(EmbeddingOptions.SectionName));

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        services.AddOpenAiEmbeddingHttpClient();
        services.AddSingleton<OpenAIEmbeddingClient>();
        services.AddSingleton<IEmbeddingClient>(static sp =>
            new CachingEmbeddingClient(
                sp.GetRequiredService<OpenAIEmbeddingClient>(),
                sp.GetRequiredService<ICacheService>(),
                sp.GetRequiredService<IOptions<EmbeddingOptions>>()));

        services.AddSingleton<PdfDocumentParser>();
        services.AddSingleton<DocxDocumentParser>();
        services.AddSingleton<PlainTextDocumentParser>();
        services.AddSingleton<IDocumentParserFactory, DocumentParserFactory>();

        services.AddSingleton<IVectorRecall, InMemoryVectorRecall>();

        return services;
    }
}
