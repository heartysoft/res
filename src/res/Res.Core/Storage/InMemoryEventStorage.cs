using System;
using System.Collections.Generic;
using System.Linq;
using Res.Client;

namespace Res.Core.Storage
{
    public class InMemoryEventStorage : EventStorage
    {
        readonly List<EventInStorage> _events = new List<EventInStorage>();
        private long _globalSequence = long.MinValue;

        public long WriteCount { get; private set; }
        public long ReadCount { get; set; }

        public CommitResults Store(CommitsForStorage commits)
        {
            var unSuccessful = new List<Guid>();

            var commitsToSave = commits.Commits.Where(x => x.Events.Length > 0);
            var entries = commitsToSave.OrderBy(x => x.Events.Min(y => y.Timestamp));

            foreach (var commit in entries)
            {
                var expectedVersion = getExpectedVersion(commit);
                var versionForCommit = commit.Events.Length > 0 ? commit.Events.Min(x => x.Sequence) : 0;

                if (expectedVersion != versionForCommit && versionForCommit != ExpectedVersion.Any)
                {
                    unSuccessful.Add(commit.CommitId);
                    continue;
                }

                for (int index = 0; index < commit.Events.Length; index++)
                {
                    var e = commit.Events[index];

                    var version = e.Sequence == -1 ? expectedVersion + index : e.Sequence;

                    _globalSequence++;

                    _events.Add(new EventInStorage(e.EventId, commit.Context, commit.Stream, 
                        version,
                        _globalSequence,
                        e.Timestamp,
                        e.TypeKey, e.Body, e.Headers));
                }
            }

            var successful = commits.Commits.Where(x => unSuccessful.Contains(x.CommitId) == false).Select(x => x.CommitId).ToArray();

            ++WriteCount;

            return new CommitResults(successful.ToArray(), unSuccessful.ToArray());
        }

        private long getExpectedVersion(CommitForStorage commit)
        {
            CommitForStorage commit1 = commit;
            var eventsForStream = _events.Where(
                x => x.Context.Equals(commit1.Context) && x.Stream.Equals(commit1.Stream)).ToArray();

            long expectedVersion = 1;

            if (eventsForStream.Any())
                expectedVersion = eventsForStream.Max(x => x.Sequence) + 1;
            return expectedVersion;
        }

        public EventInStorage[] FetchEventsBetween(DateTime fromInclusive, DateTime toInclusive)
        {
            return _events.Where(x => x.Timestamp >= fromInclusive && x.Timestamp <= toInclusive)
                .OrderBy(x => x.Timestamp)
                .ThenBy(x => x.Sequence)
                .ToArray();
        }

        public EventInStorage[] LoadEvents(string context, string streamId, long fromVersion = 0, long? maxVersion = null)
        {
            return _events.Where(x => x.Context.Equals(context) && x.Stream.Equals(streamId)
                                      && x.Sequence >= fromVersion && (!maxVersion.HasValue || x.Sequence <= maxVersion.Value)
                ).ToArray();
        }

        public Dictionary<Guid, EventInStorage> FetchEvent(FetchEventRequest[] request)
        {
            var results = _events.Join(request, x => new Tuple<Guid, string, object>(x.EventId, x.Context, x.Stream),
                x => new Tuple<Guid, string, object>(x.EventId, x.Context, x.Stream),
                (x, y) => new KeyValuePair<Guid, EventInStorage>(y.RequestId, x))
                .ToDictionary(x => x.Key, x => x.Value);

            ReadCount++;

            return results;
        }

        public void Verify()
        {

        }
    }
}