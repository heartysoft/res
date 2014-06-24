using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Res.Core.Storage;

namespace Res.Core.StorageBuffering
{
    public class EventStorageReader
    {
        private readonly int _maxSize;
        private readonly TimeSpan _maxAgeBeforeDrop;
        private readonly EventStorage _storage;
        private readonly int _maxBatchSize;
        readonly ConcurrentQueue<Entry> _queue = new ConcurrentQueue<Entry>();

        public EventStorageReader(int maxSize, TimeSpan maxAgeBeforeDrop, EventStorage storage, int maxBatchSize = 2048)
        {
            _maxSize = maxSize;
            _maxAgeBeforeDrop = maxAgeBeforeDrop;
            _storage = storage;
            _maxBatchSize = maxBatchSize;
        }

        public Task Start(CancellationToken token)
        {
            return Task.Factory.StartNew(() => run(token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public Task<EventInStorage> Fetch(FetchEventRequest request)
        {
            if (_queue.Count >= _maxSize)
                throw new StorageReaderBusyException(_maxSize);

            var entry = new Entry(request, _maxAgeBeforeDrop);
            _queue.Enqueue(entry);

            return entry.Task;
        }

        public EventInStorage[] LoadEventsForStream(string contextId, string streamId, long fromVersion = 0, long? maxVersion = null)
        {
            return _storage.LoadEvents(contextId, streamId, fromVersion, maxVersion);
        }

        private void fetch(Entry[] entries)
        {
            var requests = entries.ToDictionary(x => x.Request.RequestId, x => x);
            var parameter = requests.Values.Select(x => x.Request).ToArray();

            try
            {
                var results = _storage.FetchEvent(parameter);

                //these can't fail as they're try operations.
                foreach (var requestId in requests.Keys)
                {
                    if (results.ContainsKey(requestId) == false)
                        requests[requestId].SignalNotFound();
                    else
                        requests[requestId].SignalResult(results[requestId]);
                }
            }
            catch (Exception e)
            {
                foreach (var entry in entries)
                    entry.Fail(e);
            }
        }

        private void run(CancellationToken token)
        {
            while (token.IsCancellationRequested == false)
            {
                var list = new List<Entry>();

                while (list.Count < _maxBatchSize)
                {
                    Entry entry;

                    if (_queue.TryDequeue(out entry) == false)
                        break;

                    if (entry.ShouldDrop())
                    {
                        entry.Harikiri();
                    }

                    list.Add(entry);
                }

                if (list.Count > 0)
                {
                    fetch(list.ToArray());
                    list.Clear();
                }
            }
        }

        public class StorageReaderBusyException : Exception
        {
            public StorageReaderBusyException(int maxSize)
                : base(
                    string.Format(
                        "The storage reader has a max pending size of {0} commits, which has been reached. Please try again later. If seen consistently, please implement a backoff strategy.",
                        maxSize
                        ))
            {
            }
        }

        private class Entry
        {
            public FetchEventRequest Request { get; private set; }
            public Task<EventInStorage> Task { get { return _task.Task; } }

            private readonly TimeSpan _maxAge;
            private readonly DateTime _dropTime;
            private readonly TaskCompletionSource<EventInStorage> _task;

            public Entry(FetchEventRequest request, TimeSpan maxAge)
            {
                Request = request;
                _maxAge = maxAge;
                _dropTime = DateTime.Now.Add(maxAge);
                _task = new TaskCompletionSource<EventInStorage>();
            }

            public bool ShouldDrop()
            {
                return DateTime.Now >= _dropTime;
            }

            public void Harikiri()
            {
                _task.TrySetException(new StorageReaderTimeoutException(Request.RequestId, _maxAge));
            }

            public void Fail(Exception exception)
            {
                _task.TrySetException(exception);
            }

            public void SignalNotFound()
            {
                _task.TrySetException(new EventNotFoundException(Request));
            }

            public void SignalResult(EventInStorage eventInStorage)
            {
                _task.TrySetResult(eventInStorage);
            }
        }

        public class EventNotFoundException : Exception
        {
            public FetchEventRequest Request { get; private set; }

            public EventNotFoundException(FetchEventRequest request) :
                base(string.Format("The event with context: {0}, stream: {1}, eventId: {2} was not found.", request.Context, request.Stream, request.EventId))
            {
                Request = request;
            }
        }

        public class StorageReaderTimeoutException : Exception
        {
            public StorageReaderTimeoutException(Guid requestId, TimeSpan maxAge)
                : base(
                    string.Format("The request with id {0} timed out waiting for the storage reader to read from storage. The timeout is {1}.", requestId, maxAge))
            {
            }
        }
    }
}