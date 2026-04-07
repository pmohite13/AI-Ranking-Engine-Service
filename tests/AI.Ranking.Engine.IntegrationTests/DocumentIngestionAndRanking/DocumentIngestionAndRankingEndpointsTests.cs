using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using AI.Ranking.Engine.Api.Contracts;
using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AI.Ranking.Engine.IntegrationTests.DocumentIngestionAndRanking;

public sealed class DocumentIngestionAndRankingEndpointsTests
    : IClassFixture<DocumentIngestionAndRankingEndpointsTests.DocumentIngestionAndRankingWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocumentIngestionAndRankingEndpointsTests(DocumentIngestionAndRankingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_ingest_candidate_returns_200()
    {
        using var content = BuildMultipart(
            entityId: "candidate-1",
            kind: IngestionEntityKind.Candidate,
            fileName: "candidate.pdf",
            text: "candidate profile text");

        var response = await _client.PostAsync("/api/v1/documents/ingest", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_rank_unknown_job_returns_404()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/jobs/missing-job/rank", new RankJobRequest(10, 5));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_rank_after_ingest_returns_top_candidate()
    {
        using var candidateIngest = BuildMultipart(
            entityId: "candidate-42",
            kind: IngestionEntityKind.Candidate,
            fileName: "candidate.pdf",
            text: "candidate resume");
        using var jobIngest = BuildMultipart(
            entityId: "job-42",
            kind: IngestionEntityKind.Job,
            fileName: "job.pdf",
            text: "job description");

        var candidateResponse = await _client.PostAsync("/api/v1/documents/ingest", candidateIngest);
        var jobResponse = await _client.PostAsync("/api/v1/documents/ingest", jobIngest);

        Assert.Equal(HttpStatusCode.OK, candidateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, jobResponse.StatusCode);

        var rankResponse = await _client.PostAsJsonAsync("/api/v1/jobs/job-42/rank", new RankJobRequest(10, 5));
        Assert.Equal(HttpStatusCode.OK, rankResponse.StatusCode);

        var payload = await rankResponse.Content.ReadFromJsonAsync<RankJobResponse>();
        Assert.NotNull(payload);
        Assert.Single(payload.Results);
        Assert.Equal("candidate-42", payload.Results[0].CandidateId);
    }

    private static MultipartFormDataContent BuildMultipart(string entityId, IngestionEntityKind kind, string fileName, string text)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(entityId), "EntityId");
        content.Add(new StringContent(kind.ToString()), "EntityKind");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(text));
        fileContent.Headers.ContentType = new("application/pdf");
        content.Add(fileContent, "File", fileName);
        return content;
    }

    public sealed class DocumentIngestionAndRankingWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDocumentParserFactory>();
                services.RemoveAll<ILLMStructuredExtractor>();
                services.RemoveAll<IEmbeddingClient>();

                services.AddSingleton<IDocumentParserFactory, FakeParserFactory>();
                services.AddSingleton<ILLMStructuredExtractor, FakeStructuredExtractor>();
                services.AddSingleton<IEmbeddingClient, FakeEmbeddingClient>();
            });
        }
    }

    private sealed class FakeParserFactory : IDocumentParserFactory
    {
        private static readonly IDocumentParser Parser = new FakeParser();

        public IDocumentParser GetParser(DocumentContentType contentType) => Parser;
    }

    private sealed class FakeParser : IDocumentParser
    {
        public bool CanParse(DocumentContentType contentType) => true;

        public async Task<ParsedDocument> ParseAsync(
            Stream stream,
            DocumentParseInput input,
            CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            var bytes = ms.ToArray();
            return new ParsedDocument
            {
                ContentSha256 = SHA256.HashData(bytes),
                ContentType = input.ContentType,
                NormalizedText = Encoding.UTF8.GetString(bytes),
            };
        }
    }

    private sealed class FakeStructuredExtractor : ILLMStructuredExtractor
    {
        public Task<StructuredFeatures> ExtractFromResumeAsync(string normalizedText, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StructuredFeatures(
                Skills: new[] { "c#", "dotnet" },
                YearsExperience: 5,
                NormalizedRoleTitle: "software engineer"));
        }

        public Task<StructuredFeatures> ExtractFromJobAsync(string normalizedText, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StructuredFeatures(
                Skills: new[] { "c#", "dotnet" },
                YearsExperience: 3,
                NormalizedRoleTitle: "software engineer",
                MinimumYears: 3,
                MaximumYears: 8));
        }
    }

    private sealed class FakeEmbeddingClient : IEmbeddingClient
    {
        public Task<IReadOnlyList<EmbeddingVector>> EmbedAsync(
            IReadOnlyList<string> texts,
            EmbeddingRequestOptions options,
            CancellationToken cancellationToken = default)
        {
            var result = texts
                .Select(text =>
                {
                    var vector = text.Contains("candidate", StringComparison.OrdinalIgnoreCase)
                        ? new[] { 1.0f, 0.0f }
                        : new[] { 1.0f, 0.0f };
                    return new EmbeddingVector(vector, EstimatedTokenCount: 1);
                })
                .ToArray();

            return Task.FromResult<IReadOnlyList<EmbeddingVector>>(result);
        }
    }
}
