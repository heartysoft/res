using Res.Core.TcpTransport.Queues;

namespace Res.Core.Storage
{
    public interface QueueStorage
    {
        QueuedEvents Subscribe(SubscribeToQueue request);
        QueuedEvents AcknowledgeAndFetchNext(AcknowledgeQueue ack);
    }
}