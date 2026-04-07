using AI.Ranking.Engine.Api.Contracts;
using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Application.Contracts;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
        IDocumentParserFactory parserFactory,
        ILLMStructuredExtractor structuredExtractor,
        IEmbeddingClient embeddingClient,
        IVectorRecall vectorRecall,
        IProfileCatalog catalog,
        IOptions<EmbeddingOptions> embeddingOptions,
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

        await using var stream = form.File.OpenReadStream();
        var parser = parserFactory.GetParser(contentType);
        var parsed = await parser.ParseAsync(
                stream,
                new DocumentParseInput(form.File.FileName, contentType, form.File.Length),
                cancellationToken)
            .ConfigureAwait(false);

        var features = form.EntityKind == IngestionEntityKind.Candidate
            ? await structuredExtractor.ExtractFromResumeAsync(parsed.NormalizedText, cancellationToken).ConfigureAwait(false)
            : await structuredExtractor.ExtractFromJobAsync(parsed.NormalizedText, cancellationToken).ConfigureAwait(false);

        var embeddingResult = await embeddingClient
            .EmbedAsync(
                new[] { parsed.NormalizedText },
                new EmbeddingRequestOptions(
                    embeddingOptions.Value.DefaultModelId,
                    embeddingOptions.Value.DefaultDimensions),
                cancellationToken)
            .ConfigureAwait(false);

        var embedding = embeddingResult[0].Values;

        if (form.EntityKind == IngestionEntityKind.Candidate)
        {
            var candidate = new CandidateProfile
            {
                Id = form.EntityId.Trim(),
                Features = features,
            };

            await catalog.UpsertCandidateAsync(candidate, cancellationToken).ConfigureAwait(false);
            await vectorRecall.UpsertCandidateAsync(candidate.Id, embedding, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var job = new JobProfile
            {
                Id = form.EntityId.Trim(),
                Features = features,
                NormalizedDocumentText = parsed.NormalizedText,
            };

            await catalog.UpsertJobAsync(job, embedding, cancellationToken).ConfigureAwait(false);
        }

        return Results.Ok(
            new IngestDocumentResponse(
                EntityId: form.EntityId.Trim(),
                EntityKind: form.EntityKind,
                FileName: form.File.FileName,
                ContentType: contentType.ToString(),
                EmbeddingDimensions: embedding.Length,
                SkillCount: features.Skills.Count));
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
}
