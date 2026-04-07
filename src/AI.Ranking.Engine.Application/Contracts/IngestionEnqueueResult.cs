namespace AI.Ranking.Engine.Application.Contracts;

public sealed record IngestionEnqueueResult(
    bool Accepted,
    int QueueDepth,
    Task<IngestionProcessResult>? Completion,
    string? RejectionReason)
{
    public static IngestionEnqueueResult Rejected(int queueDepth, string reason) =>
        new(false, queueDepth, null, reason);
}
