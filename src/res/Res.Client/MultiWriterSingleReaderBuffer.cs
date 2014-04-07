using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace Res.Client
{
    public class MultiWriterSingleReaderBuffer
    {
        private readonly int _capacity;
        private readonly ConcurrentQueue<Entry> _queue;

        public MultiWriterSingleReaderBuffer(int maxCapacityAsPowerOfTwo)
        {
            _capacity = (int) Math.Pow(2, maxCapacityAsPowerOfTwo);
            _queue = new ConcurrentQueue<Entry>();
        }

        public Task<T> Enqueue<T>(ResRequest request) where T : ResResult
        {
            if (_queue.Count > _capacity)
            {
                throw new InternalBufferOverflowException();
            }

            var tcs = new TaskCompletionSource<T>();
            var entry = new Entry<T>(request, tcs);
            _queue.Enqueue(entry);

            return tcs.Task;
        }

        public bool TryDequeue(out Entry entry)
        {
            return _queue.TryDequeue(out entry);
        }

        public interface Entry
        {
        }

        public class Entry<T> : Entry where T : ResResult
        {
            private readonly ResRequest _request;
            private readonly TaskCompletionSource<T> _tcs;

            public Entry(ResRequest request, TaskCompletionSource<T> tcs)
            {
                _request = request;
                _tcs = tcs;
            }
        }
    }
}