using AI.Ranking.Engine.Application.DependencyInjection;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<IngestOptions>(builder.Configuration.GetSection(IngestOptions.SectionName));
builder.Services.Configure<RankingConstraintsOptions>(builder.Configuration.GetSection(RankingConstraintsOptions.SectionName));
builder.Services.AddRankingEngineApplication();
builder.Services.AddRankingEngineInfrastructure();

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
