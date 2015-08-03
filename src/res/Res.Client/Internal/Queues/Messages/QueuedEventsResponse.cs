using System;

namespace Res.Client.Internal.Queues.Messages
{
    public class QueuedEventsResponse : ResResponse
    {
        public string Context { get; private set; }
        public string QueueId { get; private set; }
        public string SubscriberId { get; private set; }
        public DateTime TimeOfResponse { get; private set; }
        public long? AllocationId { get; private set; }
        public EventInStorage[] Events { get; private set; }

        public QueuedEventsResponse(string context, string queueId, string subscriberId, DateTime timeOfResponse, long? allocationId, EventInStorage[] events)
        {
            Context = context;
            QueueId = queueId;
            SubscriberId = subscriberId;
            TimeOfResponse = timeOfResponse;
            AllocationId = allocationId;
            Events = events;
        }
    }
}