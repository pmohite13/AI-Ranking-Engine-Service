using AI.Ranking.Engine.Application.DependencyInjection;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<IngestOptions>(builder.Configuration.GetSection(IngestOptions.SectionName));
builder.Services.Configure<RankingConstraintsOptions>(builder.Configuration.GetSection(RankingConstraintsOptions.SectionName));
builder.Services.AddSingleton<IOptions<RankingWeights>>(_ =>
{
    var section = builder.Configuration.GetSection("RankingWeights");
    var defaults = RankingWeights.Default;
    var weights = new RankingWeights(
        Semantic: section.GetValue<double?>("Semantic") ?? defaults.Semantic,
        SkillOverlap: section.GetValue<double?>("SkillOverlap") ?? defaults.SkillOverlap,
        ExperienceFit: section.GetValue<double?>("ExperienceFit") ?? defaults.ExperienceFit,
        Keyword: section.GetValue<double?>("Keyword") ?? defaults.Keyword);
    weights.EnsureValid();
    return Options.Create(weights);
});
builder.Services.AddRankingEngineApplication();
builder.Services.AddRankingEngineInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new HealthResponse(Status: "healthy")))
    .WithName("Health")
    .WithOpenApi()
    .WithTags("Health");

app.Run();

internal sealed record HealthResponse(string Status);

/// <summary>
/// Exposes the implicit Program type to the integration test assembly (<see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}" />).
/// </summary>
public partial class Program
{
}
