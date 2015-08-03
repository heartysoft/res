using System;

namespace Res.Core.Storage
{
    public class QueueAllocation
    {
        public long AllocationId { get; private set; }
        public string QueueId { get; private set; }
        public string Context { get; private set; }
        public string SubscriberId { get; private set; }
        public DateTime ExpiresAtUtc { get; private set; }
        public long StartMarker { get; private set; }
        public long EndMarker { get; private set; }

        public QueueAllocation(long allocationId, string queueId, string context, string subscriberId, DateTime expiresAtUtc, long startMarker, long endMarker)
        {
            AllocationId = allocationId;
            QueueId = queueId;
            Context = context;
            SubscriberId = subscriberId;
            ExpiresAtUtc = expiresAtUtc;
            StartMarker = startMarker;
            EndMarker = endMarker;
        }

        public bool MatchesQueueAndSubscriber(string context, string queueId, string subscriberId)
        {
            return QueueId.Equals(queueId) && Context.Equals(context) && SubscriberId.Equals(subscriberId);
        }

        public bool MatchesQueue(string context, string queueId)
        {
            return QueueId.Equals(queueId) && Context.Equals(context);
        }

        public bool HasExpired(DateTime utcNow)
        {
            return ExpiresAtUtc <= utcNow;
        }

        public QueueAllocation ReAllocate(string subscriberId, DateTime expiresAt)
        {
            return new QueueAllocation(AllocationId, QueueId, Context, subscriberId, expiresAt, StartMarker, EndMarker);
        }

        public bool WithinTimeRange(EventInStorage eventInStorage)
        {
            return StartMarker <= eventInStorage.GlobalSequence && eventInStorage.GlobalSequence <= EndMarker;
        }
    }
}