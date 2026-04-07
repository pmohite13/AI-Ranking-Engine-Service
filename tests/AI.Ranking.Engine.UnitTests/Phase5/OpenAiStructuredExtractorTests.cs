using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Infrastructure.Caching;
using AI.Ranking.Engine.Infrastructure.Extraction;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.UnitTests.Phase5;

public sealed class OpenAiStructuredExtractorTests
{
    [Fact]
    public async Task ExtractFromJobAsync_uses_heuristic_fallback_when_llm_returns_null()
    {
        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        var extractor = new OpenAiStructuredExtractor(
            llmClient: new FakeStructuredLlmClient(payload: null),
            cache,
            Options.Create(new LlmExtractionOptions()),
            new HeuristicStructuredFeatureExtractor(),
            NullLogger<OpenAiStructuredExtractor>.Instance);

        var text = "Role: Senior Backend Engineer. 5-7 years required. Skills: C#, .NET, SQL, Azure.";
        var result = await extractor.ExtractFromJobAsync(text);

        Assert.Contains("c#", result.Skills);
        Assert.Contains("sql", result.Skills);
        Assert.Equal(5, result.MinimumYears);
        Assert.Equal(7, result.MaximumYears);
    }

    [Fact]
    public async Task ExtractFromResumeAsync_merges_llm_and_fallback_and_normalizes_values()
    {
        var llmPayload = new StructuredExtractionPayload(
            Skills: new List<string> { "C Sharp", "SQL" },
            YearsExperience: 4.5,
            RoleTitle: "Senior Backend Engineer",
            MinimumYears: 10,
            MaximumYears: 2);

        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        var extractor = new OpenAiStructuredExtractor(
            llmClient: new FakeStructuredLlmClient(llmPayload),
            cache,
            Options.Create(new LlmExtractionOptions()),
            new HeuristicStructuredFeatureExtractor(),
            NullLogger<OpenAiStructuredExtractor>.Instance);

        var text = "Current role: Backend Engineer. 3+ years in .NET and Azure.";
        var result = await extractor.ExtractFromResumeAsync(text);

        Assert.Contains("c#", result.Skills);
        Assert.Contains("sql", result.Skills);
        Assert.Equal(4.5, result.YearsExperience);
        Assert.Equal("senior backend engineer", result.NormalizedRoleTitle);
        Assert.Null(result.MinimumYears);
        Assert.Null(result.MaximumYears);
    }

    private sealed class FakeStructuredLlmClient : IStructuredLlmClient
    {
        private readonly StructuredExtractionPayload? _payload;

        public FakeStructuredLlmClient(StructuredExtractionPayload? payload)
        {
            _payload = payload;
        }

        public Task<StructuredExtractionPayload?> ExtractAsync(
            string normalizedText,
            ExtractionDocumentKind kind,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_payload);
        }
    }
}
