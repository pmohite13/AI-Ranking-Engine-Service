using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Infrastructure.Caching;
using AI.Ranking.Engine.Infrastructure.Embeddings;
using AI.Ranking.Engine.Infrastructure.Extraction;
using AI.Ranking.Engine.Infrastructure.Http;
using AI.Ranking.Engine.Infrastructure.Ingestion;
using AI.Ranking.Engine.Infrastructure.Parsing;
using AI.Ranking.Engine.Infrastructure.Storage;
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
        services.Configure<LlmExtractionOptions>(configuration.GetSection(LlmExtractionOptions.SectionName));
        services.Configure<IngestionQueueOptions>(configuration.GetSection(IngestionQueueOptions.SectionName));

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        services.AddOpenAiEmbeddingHttpClient();
        services.AddOpenAiStructuredExtractionHttpClient();
        services.AddSingleton<OpenAIEmbeddingClient>();
        services.AddSingleton<IStructuredLlmClient, OpenAiStructuredLlmClient>();
        services.AddSingleton<HeuristicStructuredFeatureExtractor>();
        services.AddSingleton<ILLMStructuredExtractor, OpenAiStructuredExtractor>();
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
        services.AddSingleton<IProfileCatalog, InMemoryProfileCatalog>();
        services.AddSingleton<IIngestionIdempotencyStore, InMemoryIngestionIdempotencyStore>();
        services.AddSingleton<ChannelIngestionQueue>();
        services.AddSingleton<IIngestionQueue>(static sp => sp.GetRequiredService<ChannelIngestionQueue>());
        services.AddSingleton<IIngestionQueueReader>(static sp => sp.GetRequiredService<ChannelIngestionQueue>());
        services.AddHostedService<IngestionBackgroundWorker>();

        return services;
    }
}
