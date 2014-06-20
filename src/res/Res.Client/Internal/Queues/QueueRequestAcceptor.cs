using System;
using System.Threading.Tasks;
using Res.Client.Internal.Queues.Messages;

namespace Res.Client.Internal.Queues
{
    public class QueueRequestAcceptor
    {
        private readonly MultiWriterSingleReaderBuffer _buffer;

        public QueueRequestAcceptor(MultiWriterSingleReaderBuffer buffer)
        {
            _buffer = buffer;
        }

        public Task<QueuedEventsResponse> Subscribe(SubscribeToQueueRequest request, TimeSpan timeout)
        {
            return _buffer.Enqueue<QueuedEventsResponse>(request, DateTime.Now.Add(timeout));
        }

        public Task<QueuedEventsResponse> AcknowledgeAndFetchNext(AcknowledgeQueueAndFetchNextRequest request,
            TimeSpan timeout)
        {
            return _buffer.Enqueue<QueuedEventsResponse>(request, DateTime.Now.Add(timeout));
        }
    }
}