namespace AI.Ranking.Engine.Infrastructure.Ingestion;

public sealed class IngestionQueueOptions
{
    public const string SectionName = "IngestionQueue";

    public int Capacity { get; set; } = 256;

    public int WorkerCount { get; set; } = 4;

    public int DepthLogIntervalSeconds { get; set; } = 15;
}
