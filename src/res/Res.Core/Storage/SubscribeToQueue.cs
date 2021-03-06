using System;

namespace Res.Core.Storage
{
    public class SubscribeToQueue
    {
        public string QueueId { get; private set; }
        public string SubscriberId { get; private set; }
        public string Context { get; private set; }
        public string Filter { get; private set; }
        public DateTime UtcQueueStartTime { get; private set; }
        public int AllocationSize { get; private set; }
        public int AllocationTimeoutInMilliseconds { get; private set; }

        public SubscribeToQueue(string context, string queueId, string subscriberId, string filter, DateTime utcQueueStartTime, int allocationSize, int allocationTimeoutInMilliseconds)
        {
            QueueId = queueId;
            SubscriberId = subscriberId;
            Context = context;
            Filter = filter;
            UtcQueueStartTime = utcQueueStartTime;
            AllocationSize = allocationSize;
            AllocationTimeoutInMilliseconds = allocationTimeoutInMilliseconds;
        }
    }
}