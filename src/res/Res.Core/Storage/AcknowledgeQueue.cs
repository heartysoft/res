namespace Res.Core.Storage
{
    public class AcknowledgeQueue
    {
        public string QueueId { get; private set; }
        public string Context { get; private set; }
        public string SubscriberId { get; private set; }
        public long? AllocationId { get; private set; }
        public int AllocationSize { get; private set; }
        public int AllocationTimeInMilliseconds { get; private set; }

        public AcknowledgeQueue(string context, string queueId, string subscriberId, long? allocationId, int allocationSize, int allocationTimeInMilliseconds)
        {
            QueueId = queueId;
            Context = context;
            SubscriberId = subscriberId;
            AllocationId = allocationId;
            AllocationSize = allocationSize;
            AllocationTimeInMilliseconds = allocationTimeInMilliseconds;
        }
    }
}