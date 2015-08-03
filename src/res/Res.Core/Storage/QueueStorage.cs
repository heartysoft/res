using System;
using Res.Core.TcpTransport.Queues;

namespace Res.Core.Storage
{
    public interface QueueStorage
    {
        QueuedEvents Subscribe(SubscribeToQueue request);
        QueuedEvents AcknowledgeAndFetchNext(AcknowledgeQueue ack);
        QueueStorageInfo[] GetAllByDecreasingNextMarker(int count, int skip);
    }

    public class QueueStorageInfo
    {
        public string QueueId { get; private set; }
        public string Context { get; private set; }
        public string Filter { get; private set; }
        public long NextMarker { get; private set; }

        public QueueStorageInfo(string queueId, string context, string filter, long nextMarker)
        {
            QueueId = queueId;
            Context = context;
            Filter = filter;
            NextMarker = nextMarker;
        }

        public bool Matches(string queueId, string context, string filter)
        {
            return QueueId.Equals(queueId) && Context.Equals(context) && Filter.Equals(filter);
        }

        public bool MatchesQueue(string queueId)
        {
            return QueueId.Equals(queueId);
        }

        public bool MatchesEvent(EventInStorage eventInStorage)
        {
            if (!MatchesContextAndFilter(eventInStorage.Context, eventInStorage.Stream)) return false;
            if (eventInStorage.GlobalSequence < NextMarker) return false;

            return true;
        }

        public QueueStorageInfo WithNextMarker(long value)
        {
            return new QueueStorageInfo(QueueId, Context, Filter, value);
        }

        public bool MatchesContextAndFilter(string context, string stream)
        {
            if (!Context.Equals(context)) return false;
            if (Filter != "*" && stream.StartsWith(Filter) == false) return false;
            return true;
        }
    }

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

        public bool MatchesQueueAndSubscriber(string queueId, string context, string subscriberId)
        {
            return QueueId.Equals(queueId) && Context.Equals(context) && SubscriberId.Equals(subscriberId);
        }

        public bool MatchesQueue(string queueId, string context)
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