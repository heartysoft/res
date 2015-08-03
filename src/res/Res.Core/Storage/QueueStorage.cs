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

    public class QueueAlreadyExistsInContextWithDifferentFilterException
        : Exception
    {
        public QueueAlreadyExistsInContextWithDifferentFilterException(string context, string queueId, string filter) : base(
            string.Format("A queue named [{0}] already exists in context [{1}] with a different filter. Your requested filter is [{2}]. Within a context, queue ids are unique.", queueId, context, filter))
        {
        }
    }
}