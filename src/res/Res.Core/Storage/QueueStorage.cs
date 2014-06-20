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
        public string QueueId { get; set; }
        public string Context { get; set; }
        public string Filter { get; set; }
        public long NextMarker { get; set; }

        public QueueStorageInfo(string queueId, string context, string filter, long nextMarker)
        {
            QueueId = queueId;
            Context = context;
            Filter = filter;
            NextMarker = nextMarker;
        }
    }

    public class QueueAllocation
    {
        public string QueueId { get; private set; }
        public string SubscriberId { get; private set; }
        public DateTime ExpiresAtUtc { get; private set; }
        public long StartMarker { get; private set; }
        public long EndMarker { get; private set; }

        public QueueAllocation(string queueId, string subscriberId, DateTime expiresAtUtc, long startMarker, long endMarker)
        {
            QueueId = queueId;
            SubscriberId = subscriberId;
            ExpiresAtUtc = expiresAtUtc;
            StartMarker = startMarker;
            EndMarker = endMarker;
        }
    }
}