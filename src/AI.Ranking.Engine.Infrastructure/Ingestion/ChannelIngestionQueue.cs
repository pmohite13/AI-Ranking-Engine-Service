using System.Threading.Channels;
using AI.Ranking.Engine.Application.Abstractions;
using AI.Ranking.Engine.Application.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.Infrastructure.Ingestion;

public sealed class ChannelIngestionQueue : IIngestionQueue, IIngestionQueueReader
{
    private readonly Channel<IngestionQueueMessage> _channel;
    private readonly ILogger<ChannelIngestionQueue> _logger;
    private int _depth;

    public ChannelIngestionQueue(
        IOptions<IngestionQueueOptions> options,
        ILogger<ChannelIngestionQueue> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger;

        var capacity = Math.Max(1, options.Value.Capacity);
        _channel = Channel.CreateBounded<IngestionQueueMessage>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });
    }

    public int ApproximateDepth => Volatile.Read(ref _depth);

    ChannelReader<IngestionQueueMessage> IIngestionQueueReader.Reader => _channel.Reader;

    public async ValueTask<IngestionEnqueueResult> EnqueueAsync(
        IngestionWorkItem workItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        var completion = new TaskCompletionSource<IngestionProcessResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var message = new IngestionQueueMessage
        {
            WorkItem = workItem,
            CompletionSource = completion,
        };

        try
        {
            await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
            var depth = Interlocked.Increment(ref _depth);
            _logger.LogDebug("Ingestion item enqueued. Queue depth: {QueueDepth}", depth);
            return new IngestionEnqueueResult(
                Accepted: true,
                QueueDepth: depth,
                Completion: completion.Task,
                RejectionReason: null);
        }
        catch (ChannelClosedException)
        {
            return IngestionEnqueueResult.Rejected(ApproximateDepth, "Ingestion queue is closed.");
        }
        catch (OperationCanceledException)
        {
            return IngestionEnqueueResult.Rejected(ApproximateDepth, "Ingestion queue enqueue was cancelled.");
        }
    }

    public void OnDequeued()
    {
        var depth = Interlocked.Decrement(ref _depth);
        if (depth < 0)
        {
            Interlocked.Exchange(ref _depth, 0);
            depth = 0;
        }

        _logger.LogDebug("Ingestion item dequeued. Queue depth: {QueueDepth}", depth);
    }
}
