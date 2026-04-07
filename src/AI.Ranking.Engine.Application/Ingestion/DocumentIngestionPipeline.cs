using System.Security.Cryptography;
using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Application.Contracts;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Domain.Entities;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.Application.Ingestion;

public sealed class DocumentIngestionPipeline : IDocumentIngestionPipeline
{
    private const string IngestionDedupeCacheKeyPrefix = "ingest:dedupe:v1:";

    private readonly IDocumentParserFactory _parserFactory;
    private readonly ILLMStructuredExtractor _structuredExtractor;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IVectorRecall _vectorRecall;
    private readonly IProfileCatalog _catalog;
    private readonly ICacheService _cacheService;
    private readonly IOptions<EmbeddingOptions> _embeddingOptions;
    private readonly IOptions<IngestOptions> _ingestOptions;
    private readonly IIngestionIdempotencyStore _idempotencyStore;

    public DocumentIngestionPipeline(
        IDocumentParserFactory parserFactory,
        ILLMStructuredExtractor structuredExtractor,
        IEmbeddingClient embeddingClient,
        IVectorRecall vectorRecall,
        IProfileCatalog catalog,
        ICacheService cacheService,
        IOptions<EmbeddingOptions> embeddingOptions,
        IOptions<IngestOptions> ingestOptions,
        IIngestionIdempotencyStore idempotencyStore)
    {
        _parserFactory = parserFactory;
        _structuredExtractor = structuredExtractor;
        _embeddingClient = embeddingClient;
        _vectorRecall = vectorRecall;
        _catalog = catalog;
        _cacheService = cacheService;
        _embeddingOptions = embeddingOptions;
        _ingestOptions = ingestOptions;
        _idempotencyStore = idempotencyStore;
    }

    public async Task<IngestionProcessResult> ProcessAsync(IngestionWorkItem workItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        ArgumentNullException.ThrowIfNull(workItem.FileBytes);

        var hash = SHA256.HashData(workItem.FileBytes);
        var idempotencyKey = BuildIdempotencyKey(workItem, hash);
        var dedupeCacheKey = IngestionDedupeCacheKeyPrefix + idempotencyKey;

        var cached = await _cacheService
            .GetAsync<IngestionProcessResult>(dedupeCacheKey, cancellationToken)
            .ConfigureAwait(false);
        if (cached is not null)
        {
            return cached with { Deduplicated = true };
        }

        if (!_idempotencyStore.TryStart(idempotencyKey, out var existingTask))
        {
            var existingResult = await existingTask!.ConfigureAwait(false);
            return existingResult with { Deduplicated = true };
        }

        try
        {
            using var stream = new MemoryStream(workItem.FileBytes, writable: false);
            var parser = _parserFactory.GetParser(workItem.ContentType);
            var parsed = await parser.ParseAsync(
                    stream,
                    new DocumentParseInput(workItem.FileName, workItem.ContentType, workItem.FileBytes.Length),
                    cancellationToken)
                .ConfigureAwait(false);

            var features = workItem.EntityKind == IngestionEntityKind.Candidate
                ? await _structuredExtractor.ExtractFromResumeAsync(parsed.NormalizedText, cancellationToken).ConfigureAwait(false)
                : await _structuredExtractor.ExtractFromJobAsync(parsed.NormalizedText, cancellationToken).ConfigureAwait(false);

            var embeddingResult = await _embeddingClient
                .EmbedAsync(
                    new[] { parsed.NormalizedText },
                    new EmbeddingRequestOptions(
                        _embeddingOptions.Value.DefaultModelId,
                        _embeddingOptions.Value.DefaultDimensions),
                    cancellationToken)
                .ConfigureAwait(false);

            var embedding = embeddingResult[0].Values;

            if (workItem.EntityKind == IngestionEntityKind.Candidate)
            {
                var candidate = new CandidateProfile
                {
                    Id = workItem.EntityId,
                    Features = features,
                };

                await _catalog.UpsertCandidateAsync(candidate, cancellationToken).ConfigureAwait(false);
                await _vectorRecall.UpsertCandidateAsync(candidate.Id, embedding, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var job = new JobProfile
                {
                    Id = workItem.EntityId,
                    Features = features,
                    NormalizedDocumentText = parsed.NormalizedText,
                };

                await _catalog.UpsertJobAsync(job, embedding, cancellationToken).ConfigureAwait(false);
            }

            var result = new IngestionProcessResult(
                EntityId: workItem.EntityId,
                EntityKind: workItem.EntityKind,
                FileName: workItem.FileName,
                ContentType: workItem.ContentType.ToString(),
                EmbeddingDimensions: embedding.Length,
                SkillCount: features.Skills.Count,
                Deduplicated: false);

            _idempotencyStore.Complete(idempotencyKey, result);

            var cacheHours = Math.Max(1, _ingestOptions.Value.IngestionDedupCacheHours);
            await _cacheService
                .SetAsync(
                    dedupeCacheKey,
                    result,
                    TimeSpan.FromHours(cacheHours),
                    cancellationToken)
                .ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            _idempotencyStore.Fail(idempotencyKey, ex);
            throw;
        }
    }

    private string BuildIdempotencyKey(IngestionWorkItem workItem, byte[] hash)
    {
        var hashHex = Convert.ToHexString(hash);
        var modelId = _embeddingOptions.Value.DefaultModelId;
        return $"{workItem.EntityKind}:{workItem.EntityId}:{hashHex}:{modelId}";
    }
}
