using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Res.Core.Storage;
using Res.Core.TcpTransport;

namespace Res.Core.StorageBuffering
{
    public class EventStorageWriter
    {
        private readonly int _maxSize;
        private readonly TimeSpan _maxAgeBeforeDrop;
        private readonly EventStorage _storage;
        private readonly int _maxBatchSize;
        SpinWait _spinwait = new SpinWait();
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();


        readonly ConcurrentQueue<Entry> _queue = new ConcurrentQueue<Entry>();

        public EventStorageWriter(int maxSize, TimeSpan maxAgeBeforeDrop, EventStorage storage, int maxBatchSize = 2048)
        {
            _maxSize = maxSize;
            _maxAgeBeforeDrop = maxAgeBeforeDrop;
            _storage = storage;
            _maxBatchSize = maxBatchSize;
        }

        public Task Store(CommitForStorage commit)
        {
            Logger.Info("[StorageWriter] Received commit.");
            if (_queue.Count >= _maxSize)
                throw new StorageWriterBusyException(_maxSize);

            Logger.Info("[StorageWriter] Have capacity, will accept.");
            var entry = new Entry(commit, _maxAgeBeforeDrop);
            _queue.Enqueue(entry);
            Logger.InfoFormat("[StorageWriter] Commit queued. {0}", _queue.Count);
            return entry.Task;
        }

        public Task Start(CancellationToken token)
        {
            Logger.Info("[StorageWriter] Starting...I'm spinning around.....");
            return Task.Factory.StartNew(() => run(token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void run(CancellationToken token)
        {
            var list = new List<Entry>(_maxBatchSize);
            Logger.InfoFormat("[StorageWriter] Entering main loop. {0}", _maxBatchSize);


            var spinWait = new SpinWait();
            while (token.IsCancellationRequested == false)
            {
                try
                {
                    while (list.Count < _maxBatchSize)
                    {

                        Entry entry;
                        if (_queue.TryDequeue(out entry) == false)
                        {
                            break;
                        }

                        Logger.Info("[StorageWriter] Got something to write...");

                        if (entry.ShouldDrop())
                        {
                            Logger.Info("[StorageWriter] Dropping message....too old.");
                            entry.Harikiri();
                        }
                        else
                            list.Add(entry);
                    }

                    if (list.Count > 0)
                    {
                        Console.WriteLine(_maxBatchSize);
                        Console.WriteLine(_queue.Count);
                        store(list.ToArray());
                        list.Clear();
                    }
                    else
                    {
                        spinWait.SpinOnce();
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn("[StorageWriter] Error in mainloop.", e);
                }

                _spinwait.SpinOnce();
            }

            Logger.Info("[StorageWriter] Exiting. No more spinning for me today...");
        }

        private void store(Entry[] entries)
        {
            Logger.Info("[StorageWriter] Writing entries.");
            var commits = entries.ToDictionary(x => x.Commit.CommitId, x => x);
            var commit = new CommitsForStorage(commits.Values.Select(x => x.Commit).ToArray());

            try
            {
                Logger.InfoFormat("[StorageWriter] Storing {0} commits.", commit.Commits.Length);
                Console.WriteLine("[StorageWriter] Storing {0} commits.", commit.Commits.Length);
                var results = _storage.Store(commit);
                Logger.Info("[StorageWriter] Entries stored.");

                //these can't fail as they're try operations.
                foreach (var c in results.SuccessfulCommits)
                    commits[c].signalCompletion();

                foreach (var c in results.FailedDueToConcurrencyCommits)
                    commits[c].signalConcurrencyFailure();
            }
            catch (Exception e)
            {
                foreach (var entry in entries)
                    entry.Fail(e);
            }
            finally
            {
                commits.Clear();
            }
        }

        public class StorageWriterBusyException : Exception
        {
            public StorageWriterBusyException(int maxSize)
                : base(
                    string.Format(
                        "The storage writer has a max pending size of {0} commits, which has been reached. Please try again later. If seen consistently, please implement a backoff strategy.",
                        maxSize
                        ))
            {
            }
        }

        private class Entry
        {
            public Task Task { get { return _task.Task; } }

            public readonly CommitForStorage Commit;
            private readonly TimeSpan _maxAge;
            private readonly DateTime _dropTime;
            private readonly TaskCompletionSource<bool> _task;

            public Entry(CommitForStorage commit, TimeSpan maxAge)
            {
                Commit = commit;
                _maxAge = maxAge;
                _dropTime = DateTime.Now.Add(maxAge);
                _task = new TaskCompletionSource<bool>();
            }

            public bool ShouldDrop()
            {
                return DateTime.Now >= _dropTime;
            }

            public void Harikiri()
            {
                _task.TrySetException(new StorageWriterTimeoutException(Commit.CommitId, _maxAge));
            }

            public void signalCompletion()
            {
                _task.TrySetResult(true);
            }

            public void signalConcurrencyFailure()
            {
                //TODO: Add request token, maybe?
                _task.TrySetException(new ConcurrencyException());
            }

            public void Fail(Exception exception)
            {
                _task.TrySetException(exception);
            }

        }

        public class StorageWriterTimeoutException : Exception
        {
            public StorageWriterTimeoutException(Guid commitId, TimeSpan maxAge)
                : base(
                    string.Format("The commit with id {0} timed out waiting for the storage writer to write it to storage. The timeout is {1}.", commitId, maxAge))
            {
            }
        }

        public class ConcurrencyException : Exception
        {
        }
    }
}