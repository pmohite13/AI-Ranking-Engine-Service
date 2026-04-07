using AI.Ranking.Engine.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.Infrastructure.Ingestion;

public sealed class IngestionBackgroundWorker : BackgroundService
{
    private readonly IIngestionQueueReader _queueReader;
    private readonly IIngestionQueue _ingestionQueue;
    private readonly IDocumentIngestionPipeline _pipeline;
    private readonly IngestionQueueOptions _options;
    private readonly ILogger<IngestionBackgroundWorker> _logger;

    public IngestionBackgroundWorker(
        IIngestionQueueReader queueReader,
        IIngestionQueue ingestionQueue,
        IDocumentIngestionPipeline pipeline,
        IOptions<IngestionQueueOptions> options,
        ILogger<IngestionBackgroundWorker> logger)
    {
        _queueReader = queueReader;
        _ingestionQueue = ingestionQueue;
        _pipeline = pipeline;
        _logger = logger;
        _options = options.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerCount = Math.Max(1, _options.WorkerCount);
        var workers = Enumerable
            .Range(0, workerCount)
            .Select(_ => Task.Run(() => ConsumeAsync(stoppingToken), stoppingToken))
            .ToArray();

        var monitor = Task.Run(() => MonitorDepthAsync(stoppingToken), stoppingToken);
        return Task.WhenAll(workers.Append(monitor));
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queueReader.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            _queueReader.OnDequeued();

            try
            {
                var result = await _pipeline.ProcessAsync(message.WorkItem, stoppingToken).ConfigureAwait(false);
                message.CompletionSource.TrySetResult(result);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                message.CompletionSource.TrySetCanceled(stoppingToken);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process ingestion item for entity {EntityId}.", message.WorkItem.EntityId);
                message.CompletionSource.TrySetException(ex);
            }
        }
    }

    private async Task MonitorDepthAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.DepthLogIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Ingestion queue depth (approximate): {QueueDepth}", _ingestionQueue.ApproximateDepth);
        }
    }
}
