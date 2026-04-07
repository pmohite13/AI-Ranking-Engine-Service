using System.Threading.Channels;

namespace AI.Ranking.Engine.Infrastructure.Ingestion;

public interface IIngestionQueueReader
{
    ChannelReader<IngestionQueueMessage> Reader { get; }

    void OnDequeued();
}
