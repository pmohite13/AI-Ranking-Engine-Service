using AI.Ranking.Engine.Domain.Entities;

namespace AI.Ranking.Engine.Application.Abstractions;

/// <summary>
/// In-memory catalog abstraction for candidate/job profiles and job embeddings used by API orchestration.
/// </summary>
public interface IProfileCatalog
{
    Task UpsertCandidateAsync(CandidateProfile candidate, CancellationToken cancellationToken = default);

    Task UpsertJobAsync(JobProfile job, float[] jobEmbedding, CancellationToken cancellationToken = default);

    bool TryGetJob(string jobId, out JobProfile? job, out float[]? jobEmbedding);

    IReadOnlyDictionary<string, CandidateProfile> GetCandidateProfilesView();
}
