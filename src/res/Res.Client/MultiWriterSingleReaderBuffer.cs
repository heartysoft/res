using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace Res.Client
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
        ResRequest Request { get; }
        bool ShouldDrop();
        void Drop();
    }

    public class PendingResRequest<T> : PendingResRequest where T : ResResponse
    {
        public ResRequest Request { get; private set; }
        private readonly TaskCompletionSource<T> _tcs;
        private readonly DateTime _timeout;

        public PendingResRequest(ResRequest request, TaskCompletionSource<T> tcs, DateTime timeout)
        {
            Request = request;
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