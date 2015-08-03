using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;

namespace Res.Core.Storage.InMemoryQueueStorage
{
    public class InMemoryQueueStorage : QueueStorage
    {
        private long _gAllocationId = -9223372036854775808;
        private readonly InMemoryEventStorage _eventStorage;
        private readonly Dictionary<QueuePrimaryKey, QueueStorageInfo> _queues = new Dictionary<QueuePrimaryKey, QueueStorageInfo>();
        private readonly Dictionary<long, QueueAllocation> _allocations = new Dictionary<long, QueueAllocation>();
        private readonly object _locker = new object();

        public InMemoryQueueStorage(InMemoryEventStorage eventStorage)
        {
            _eventStorage = eventStorage;
        }

        public QueuedEvents Subscribe(SubscribeToQueue request)
        {
            lock (_locker)
            {
                createQueueIfNotExists(request.QueueId, request.Context, request.Filter, request.UtcQueueStartTime);
                var now = DateTime.UtcNow;
                var allocationId = subscribe_allocate(request.QueueId, request.Context, request.SubscriberId, request.AllocationSize,
                    request.AllocationTimeoutInMilliseconds, now);
                var events = subscribe_fetchEvents(allocationId);
                return new QueuedEvents(allocationId, events);
            }
        }

        private EventInStorage[] subscribe_fetchEvents(long? allocationId)
        {
            if (allocationId.HasValue == false) return new EventInStorage[0];
            if (_allocations.ContainsKey(allocationId.Value) == false) return new EventInStorage[0];
            var allocation = _allocations[allocationId.Value];
            if (_queues.ContainsKey(getQueuePrimaryKey(allocation.QueueId, allocation.Context)) == false) return new EventInStorage[0];
            var queue = _queues[getQueuePrimaryKey(allocation.QueueId, allocation.Context)];

            var events = _eventStorage.GetEventsMatchingCriteria(
                x => allocation.WithinTimeRange(x)
                     && queue.MatchesContextAndFilter(x.Context, x.Stream))
                .OrderBy(x => x.Timestamp).ToArray();

            return events;
        }

        private void createQueueIfNotExists(string queueId, string context, string filter, DateTime utcQueueStartTime)
        {
            if (_queues.Values.Any(x => x.Matches(queueId, context, filter)))
                return;
            var nextMarker = _eventStorage.GetMinSequenceMatchingCriteriaOrNull(
                x => x.Context == context
                        && x.Stream.StartsWith(filter)
                        && x.Timestamp > utcQueueStartTime) ?? -9223372036854775808;
            _queues[getQueuePrimaryKey(queueId, context)] = new QueueStorageInfo(queueId, context, filter, nextMarker);
        }

        private long? subscribe_allocate(string queueId, string context, string subscriberId, int count, int allocationTimeInMilliseconds,
            DateTime utcNow)
        {
            var allocation =
                _allocations.Values.FirstOrDefault(x => x.MatchesQueueAndSubscriber(queueId, context, subscriberId) && !x.HasExpired(utcNow));

            //allocation exists. do nothing.
            if (allocation != null) return allocation.AllocationId;

            var expiredAllocationForThisQueue =
                _allocations.Values.Where(x => x.MatchesQueue(queueId, context) && x.HasExpired(utcNow))
                    .OrderBy(x => x.StartMarker)
                    .FirstOrDefault();

            var expiresAt = utcNow.Add(TimeSpan.FromMilliseconds(allocationTimeInMilliseconds));
            //expired existing allocation. reallocate.
            if (expiredAllocationForThisQueue != null)
            {
                var newAllocation = expiredAllocationForThisQueue.ReAllocate(subscriberId, expiresAt);
                _allocations[expiredAllocationForThisQueue.AllocationId] = newAllocation;
                return expiredAllocationForThisQueue.AllocationId;
            }

            //No unexpired allocation for current subscriber, and no available expired allocation.Need new allocation.
            //select top(count)  events for queue with qId = queueId
            var queuePrimaryKey = getQueuePrimaryKey(queueId, context);
            var queue = _queues.ContainsKey(queuePrimaryKey) ? _queues[queuePrimaryKey] : null;
            if (queue == null) //queue not found. shouldn't happen, right?
                return null;

            var sequences = _eventStorage.GetEventsMatchingCriteria(
                    x => queue.MatchesEvent(x)).OrderBy(x => x.GlobalSequence)
                    .Select(x => x.GlobalSequence).Take(count).ToArray();

            if (sequences.Length == 0) //no events
                return null;

            var start = sequences.Min();
            var end = sequences.Max();
            var newQueue = queue.WithNextMarker(end + 1);
            _queues[queuePrimaryKey] = newQueue;

            var newAllocationId = _gAllocationId;
            _gAllocationId++;

            var newQueueAllocation = new QueueAllocation(newAllocationId, queueId, context, subscriberId, expiresAt, start, end);
            _allocations[newAllocationId] = newQueueAllocation;

            return newAllocationId;
        }


        public QueuedEvents AcknowledgeAndFetchNext(AcknowledgeQueue ack)
        {
            lock (_locker)
            {
                var now = DateTime.UtcNow;
                long? newAllocationId = null;

                if (ack.AllocationId.HasValue && _allocations.ContainsKey(ack.AllocationId.Value))
                {
                    _allocations.Remove(ack.AllocationId.Value);
                }

                if (ack.AllocationTimeInMilliseconds != -1)
                {
                    newAllocationId =
                        subscribe_allocate(ack.QueueId, ack.Context, ack.SubscriberId, ack.AllocationSize,
                            ack.AllocationTimeInMilliseconds,
                            now);
                }

                var events = subscribe_fetchEvents(newAllocationId);

                return new QueuedEvents(newAllocationId, events);
            }
        }


        public QueueStorageInfo[] GetAllByDecreasingNextMarker(int count, int skip)
        {
            lock (_locker)
            {
                var queues = _queues.Values.OrderBy(x => x.NextMarker).Skip(skip).Take(count).ToArray();
                return queues;
            }
        }

        private static QueuePrimaryKey getQueuePrimaryKey(string queueId, string context)
        {
            return new QueuePrimaryKey(queueId, context);
        }

        private class QueuePrimaryKey
        {
            private string QueueId { get; set; }
            private string Context { get; set; }

            public QueuePrimaryKey(string queueId, string context)
            {
                QueueId = queueId;
                Context = context;
            }

            private bool Equals(QueuePrimaryKey other)
            {
                return string.Equals(QueueId, other.QueueId) && string.Equals(Context, other.Context);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((QueuePrimaryKey) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((QueueId != null ? QueueId.GetHashCode() : 0)*397) ^ (Context != null ? Context.GetHashCode() : 0);
                }
            }

            public static bool operator ==(QueuePrimaryKey left, QueuePrimaryKey right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(QueuePrimaryKey left, QueuePrimaryKey right)
            {
                return !Equals(left, right);
            }
        }
    }
}