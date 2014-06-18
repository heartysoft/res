namespace Res.Core.TcpTransport.Queues
{
    public interface QueueStorage
    {
        QueuedEvents Subscribe(SubscribeToQueue request);
        QueuedEvents AcknowledgeAndFetchNext(AcknowledgeQueue ack);
    }
}