using System.Collections.Concurrent;
using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Domain.Entities;

namespace AI.Ranking.Engine.Infrastructure.Storage;

/// <summary>
/// Thread-safe process-local catalog for profiles and job embeddings.
/// </summary>
public sealed class InMemoryProfileCatalog : IProfileCatalog
{
    private readonly ConcurrentDictionary<string, CandidateProfile> _candidates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, (JobProfile Job, float[] Embedding)> _jobs = new(StringComparer.Ordinal);

    public Task UpsertCandidateAsync(CandidateProfile candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        cancellationToken.ThrowIfCancellationRequested();
        _candidates[candidate.Id] = candidate;
        return Task.CompletedTask;
    }

    public Task UpsertJobAsync(JobProfile job, float[] jobEmbedding, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(jobEmbedding);
        cancellationToken.ThrowIfCancellationRequested();

        _jobs[job.Id] = (job, (float[])jobEmbedding.Clone());
        return Task.CompletedTask;
    }

    public bool TryGetJob(string jobId, out JobProfile? job, out float[]? jobEmbedding)
    {
        job = null;
        jobEmbedding = null;

        if (string.IsNullOrWhiteSpace(jobId))
            return false;

        if (!_jobs.TryGetValue(jobId, out var value))
            return false;

        job = value.Job;
        jobEmbedding = (float[])value.Embedding.Clone();
        return true;
    }

    public IReadOnlyDictionary<string, CandidateProfile> GetCandidateProfilesView() => _candidates;
}
