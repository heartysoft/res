namespace Res.Core.TcpTransport.Queues
{
    public class AcknowledgeQueue
    {
        public string QueueId { get; private set; }
        public string SubscriberId { get; private set; }
        public long? AllocationId { get; private set; }
        public int AllocationSize { get; private set; }
        public int AllocationTimeInMilliseconds { get; private set; }

        public AcknowledgeQueue(string queueId, string subscriberId, long? allocationId, int allocationSize, int allocationTimeInMilliseconds)
        {
            QueueId = queueId;
            SubscriberId = subscriberId;
            AllocationId = allocationId;
            AllocationSize = allocationSize;
            AllocationTimeInMilliseconds = allocationTimeInMilliseconds;
        }
    }
}