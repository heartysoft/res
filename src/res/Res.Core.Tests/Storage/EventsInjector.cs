using System;
using System.Collections.Generic;
using Res.Core.Storage;

namespace Res.Core.Tests.Storage
{
    public class EventsInjector
    {
        private readonly EventStorage _storage;

        public EventsInjector(EventStorage storage)
        {
            _storage = storage;
        }

        public List<CommitModel> InsertEvents(Dictionary<Guid, CreateEventsMetaData> createEventDetails)
        {
            var listOfCommits = new List<CommitModel>();
            foreach (var key in createEventDetails.Keys)
            {
                var createEventMetaData = createEventDetails[key];
                var listOfEvents = new List<EventStorageInfo>();
                for (var i = 1; i <= createEventMetaData.NumberOfEvents; i++)
                {
                    var @event = new EventStorageInfo(Guid.NewGuid(), i, createEventMetaData.SameTimeStamps[i - 1], "sometype", "somebody", null);
                    listOfEvents.Add(@event);
                }
                var commit = new CommitModel(key, createEventMetaData.Context, createEventMetaData.Stream,
                    listOfEvents.ToArray());
                listOfCommits.Add(commit);

            }

            foreach (var commit in listOfCommits)
            {
                var newcommit = new CommitBuilder()
                    .NewCommit(commit.CommitId, commit.Context, commit.Stream);
                var firstEvent = commit.Events[0];
                var anotherCommitingEvent = newcommit.Event(new EventForStorage(firstEvent.EventId, firstEvent.Sequence, firstEvent.Timestamp, firstEvent.TypeKey, null, firstEvent.Body));
                if (commit.Events.Length > 1)
                {
                    for (var e = 1; e < commit.Events.Length; e++)
                    {
                        anotherCommitingEvent =
                            anotherCommitingEvent.Event(new EventForStorage(commit.Events[e].EventId,
                                commit.Events[e].Sequence, commit.Events[e].Timestamp, commit.Events[e].TypeKey, null, commit.Events[e].Body));
                    }
                }
                var builtCommit = anotherCommitingEvent.Build();
                _storage.Store(builtCommit);

            }
            return listOfCommits;
        }
    }
}