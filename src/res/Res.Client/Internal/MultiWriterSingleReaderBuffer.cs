using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using NetMQ;

namespace Res.Client.Internal
{
    public class MultiWriterSingleReaderBuffer
    {
        private readonly int _capacity;
        private readonly ConcurrentQueue<PendingResRequest> _queue;

        public MultiWriterSingleReaderBuffer(int maxCapacityAsPowerOfTwo)
        {
            _capacity = (int) Math.Pow(2, maxCapacityAsPowerOfTwo);
            _queue = new ConcurrentQueue<PendingResRequest>();
        }

        public Task<T> Enqueue<T>(ResRequest request, DateTime timeout) where T : ResResponse
        {
            if (_queue.Count > _capacity)
            {
                throw new InternalBufferOverflowException();
            }

            var tcs = new TaskCompletionSource<T>();
            var entry = new PendingResRequest<T>(request, tcs, timeout);
            _queue.Enqueue(entry);

            return tcs.Task;
        }

        public bool TryDequeue(out PendingResRequest pendingResRequest)
        {
            return _queue.TryDequeue(out pendingResRequest);
        }
    }

    public interface PendingResRequest
    {
        bool ShouldDrop();
        void Drop();
        Action<NetMQMessage> Send(NetMQSocket socket, Guid requestId);
    }

    public class PendingResRequest<T> : PendingResRequest where T : ResResponse
    {
        private readonly ResRequest _request;
        private readonly TaskCompletionSource<T> _tcs;
        private readonly DateTime _timeout;

        public PendingResRequest(ResRequest request, TaskCompletionSource<T> tcs, DateTime timeout)
        {
            _request = request;
            _tcs = tcs;
            _timeout = timeout;
        }

        public bool ShouldDrop()
        {
            return DateTime.Now >= _timeout;
        }

        public void Drop()
        {
            SetException(new RequestTimedOutPendingSendException());
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, Guid requestId)
        {
            return _request.Send(socket, this, requestId);
        }

        public void SetResult(T result)
        {
            _tcs.TrySetResult(result);
        }

        public void SetException(Exception exception)
        {
            _tcs.TrySetException(exception);
        }
    }
}