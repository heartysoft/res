using System;

namespace Res.Client.Internal.Queues.Messages
{
    public class QueuedEventsResponse : ResResponse
    {
        public string QueueId { get; private set; }
        public string SubscriberId { get; private set; }
        public DateTime TimeOfResponse { get; private set; }
        public long? AllocationId { get; private set; }
        public EventInStorage[] Events { get; private set; }

        public QueuedEventsResponse(string queueId, string subscriberId, DateTime timeOfResponse, long? allocationId, EventInStorage[] events)
        {
            QueueId = queueId;
            SubscriberId = subscriberId;
            TimeOfResponse = timeOfResponse;
            AllocationId = allocationId;
            Events = events;
        }
    }
}