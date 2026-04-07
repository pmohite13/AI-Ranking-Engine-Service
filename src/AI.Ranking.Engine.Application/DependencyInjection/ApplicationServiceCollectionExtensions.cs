using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Application.Ingestion;
using AI.Ranking.Engine.Application.Ranking;
using AI.Ranking.Engine.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Ranking.Engine.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers FluentValidation validators, hybrid ranking strategy, and related application services.
    /// </summary>
    public static IServiceCollection AddRankingEngineApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssemblyContaining<IngestRequestValidator>();
        services.AddScoped<IRankingStrategy, HybridRankingStrategy>();
        services.AddScoped<IHybridRankingService, HybridRankingService>();
        services.AddSingleton<IDocumentIngestionPipeline, DocumentIngestionPipeline>();

        return services;
    }
}
