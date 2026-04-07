using System.Buffers;
using AI.Ranking.Engine.Api.Contracts;
using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Application.Contracts;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ApiIngestionEntityKind = AI.Ranking.Engine.Api.Contracts.IngestionEntityKind;
using AppIngestionEntityKind = AI.Ranking.Engine.Application.Contracts.IngestionEntityKind;

namespace AI.Ranking.Engine.Api.Endpoints;

/// <summary>
/// HTTP surface for document ingestion (candidates and jobs) and job-centric ranking.
/// </summary>
public static class DocumentIngestionAndRankingEndpoints
{
    public static IEndpointRouteBuilder MapDocumentIngestionAndRankingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/v1/documents/ingest",
                IngestDocumentAsync)
            .WithName("IngestDocument")
            .WithTags("Documents")
            .Accepts<DocumentIngestForm>("multipart/form-data")
            .Produces<IngestDocumentResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .WithOpenApi();

        endpoints.MapPost(
                "/api/v1/jobs/{id}/rank",
                RankJobAsync)
            .WithName("RankJob")
            .WithTags("Ranking")
            .Accepts<RankJobRequest>("application/json")
            .Produces<RankJobResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        return endpoints;
    }

    private static async Task<IResult> IngestDocumentAsync(
        [FromForm] DocumentIngestForm form,
        IValidator<IngestRequest> ingestValidator,
        IOptions<IngestOptions> ingestOptions,
        IIngestionQueue ingestionQueue,
        CancellationToken cancellationToken)
    {
        if (form.File is null)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["File is required."] });

        if (!TryMapContentType(form.File.FileName, out var contentType))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { ["file"] = ["Only .pdf and .docx files are supported."] });
        }

        var ingestRequest = new IngestRequest(
            EntityId: form.EntityId.Trim(),
            FileName: form.File.FileName,
            ContentLength: form.File.Length,
            ContentType: contentType);

        var validation = await ingestValidator.ValidateAsync(ingestRequest, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Results.ValidationProblem(ToValidationDictionary(validation));

        var fileBytes = await ReadFileBytesAsync(form.File, ingestOptions.Value.MaxUploadBytes, cancellationToken).ConfigureAwait(false);
        var queueResult = await ingestionQueue.EnqueueAsync(
                new IngestionWorkItem(
                    EntityId: form.EntityId.Trim(),
                    EntityKind: ToApplicationEntityKind(form.EntityKind),
                    FileName: form.File.FileName,
                    ContentType: contentType,
                    FileBytes: fileBytes),
                cancellationToken)
            .ConfigureAwait(false);

        if (!queueResult.Accepted || queueResult.Completion is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Ingestion queue unavailable",
                detail: queueResult.RejectionReason ?? "Unable to queue ingestion request.");
        }

        var processed = await queueResult.Completion.ConfigureAwait(false);
        return Results.Ok(
            new IngestDocumentResponse(
                EntityId: processed.EntityId,
                EntityKind: ToApiEntityKind(processed.EntityKind),
                FileName: processed.FileName,
                ContentType: processed.ContentType,
                EmbeddingDimensions: processed.EmbeddingDimensions,
                SkillCount: processed.SkillCount,
                Deduplicated: processed.Deduplicated,
                QueueDepthAtEnqueue: queueResult.QueueDepth));
    }

    private static async Task<IResult> RankJobAsync(
        [FromRoute] string id,
        [FromBody] RankJobRequest request,
        IValidator<RankRequest> rankValidator,
        IHybridRankingService rankingService,
        IProfileCatalog catalog,
        CancellationToken cancellationToken)
    {
        var rankRequest = new RankRequest(id, request.VectorRecallTopK, request.FinalTopN);
        var validation = await rankValidator.ValidateAsync(rankRequest, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Results.ValidationProblem(ToValidationDictionary(validation));

        if (!catalog.TryGetJob(id, out var job, out var jobEmbedding) || job is null || jobEmbedding is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Job not found",
                detail: $"Job '{id}' has not been ingested.");
        }

        var results = await rankingService
            .RankTopCandidatesAsync(
                job,
                jobEmbedding,
                catalog.GetCandidateProfilesView(),
                request.VectorRecallTopK,
                request.FinalTopN,
                cancellationToken)
            .ConfigureAwait(false);

        var payload = results
            .Select(static x => new RankedCandidateResponse(
                CandidateId: x.CandidateId,
                Rank: x.Rank,
                TotalScore: x.TotalScore,
                Breakdown: new
                {
                    x.Breakdown.SemanticRaw,
                    x.Breakdown.SkillOverlapRaw,
                    x.Breakdown.ExperienceFitRaw,
                    x.Breakdown.KeywordRaw,
                    x.Breakdown.WeightedSemantic,
                    x.Breakdown.WeightedSkillOverlap,
                    x.Breakdown.WeightedExperienceFit,
                    x.Breakdown.WeightedKeyword,
                }))
            .ToArray();

        return Results.Ok(new RankJobResponse(id, request.VectorRecallTopK, request.FinalTopN, payload));
    }

    private static bool TryMapContentType(string fileName, out DocumentContentType contentType)
    {
        contentType = default;
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            contentType = DocumentContentType.Pdf;
            return true;
        }

        if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        {
            contentType = DocumentContentType.Docx;
            return true;
        }

        return false;
    }

    private static Dictionary<string, string[]> ToValidationDictionary(FluentValidation.Results.ValidationResult result)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var failure in result.Errors)
        {
            var key = string.IsNullOrWhiteSpace(failure.PropertyName) ? "request" : failure.PropertyName;
            if (!errors.TryGetValue(key, out var bucket))
            {
                bucket = new List<string>();
                errors[key] = bucket;
            }

            bucket.Add(failure.ErrorMessage);
        }

        return errors.ToDictionary(static x => x.Key, static x => x.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<byte[]> ReadFileBytesAsync(IFormFile file, int maxUploadBytes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        await using var stream = file.OpenReadStream();
        var initialCapacity = (int)Math.Min(Math.Min(file.Length, maxUploadBytes), int.MaxValue);
        using var ms = new MemoryStream(initialCapacity > 0 ? initialCapacity : 0);
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            var remaining = (long)maxUploadBytes;
            while (remaining > 0)
            {
                var toRead = (int)Math.Min(remaining, buffer.Length);
                var read = await stream.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;

                await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                remaining -= read;
            }

            return ms.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static AppIngestionEntityKind ToApplicationEntityKind(ApiIngestionEntityKind entityKind) =>
        entityKind == ApiIngestionEntityKind.Candidate
            ? AppIngestionEntityKind.Candidate
            : AppIngestionEntityKind.Job;

    private static ApiIngestionEntityKind ToApiEntityKind(AppIngestionEntityKind entityKind) =>
        entityKind == AppIngestionEntityKind.Candidate
            ? ApiIngestionEntityKind.Candidate
            : ApiIngestionEntityKind.Job;
}
