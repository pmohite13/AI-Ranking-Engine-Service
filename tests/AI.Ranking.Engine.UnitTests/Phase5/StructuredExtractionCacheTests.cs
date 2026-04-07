using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Infrastructure.Caching;
using AI.Ranking.Engine.Infrastructure.Extraction;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.UnitTests.Phase5;

public sealed class StructuredExtractionCacheTests
{
    [Fact]
    public async Task ExtractFromJobAsync_uses_cache_for_identical_input()
    {
        var fakeLlm = new CountingFakeStructuredLlmClient(
            new StructuredExtractionPayload(
                Skills: new List<string> { "c#", ".net" },
                YearsExperience: 6,
                RoleTitle: "Backend Engineer",
                MinimumYears: 5,
                MaximumYears: 8));

        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        var extractor = new OpenAiStructuredExtractor(
            fakeLlm,
            cache,
            Options.Create(new LlmExtractionOptions()),
            new HeuristicStructuredFeatureExtractor(),
            NullLogger<OpenAiStructuredExtractor>.Instance);

        const string Text = "Role: Backend Engineer. 5-8 years required. Skills: C#, .NET.";
        _ = await extractor.ExtractFromJobAsync(Text);
        _ = await extractor.ExtractFromJobAsync(Text);

        Assert.Equal(1, fakeLlm.CallCount);
    }

    private sealed class CountingFakeStructuredLlmClient : IStructuredLlmClient
    {
        private readonly StructuredExtractionPayload _payload;

        public int CallCount { get; private set; }

        public CountingFakeStructuredLlmClient(StructuredExtractionPayload payload)
        {
            _payload = payload;
        }

        public Task<StructuredExtractionPayload?> ExtractAsync(
            string normalizedText,
            ExtractionDocumentKind kind,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<StructuredExtractionPayload?>(_payload);
        }
    }
}
