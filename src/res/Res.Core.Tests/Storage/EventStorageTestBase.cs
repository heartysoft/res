using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Res.Core.Storage;

namespace Res.Core.Tests.Storage
{
    [TestFixture]
    public abstract class EventStorageTestBase
    {
        private EventStorage _storage;
        protected abstract EventStorage GetStorage();


        [Test]
        public void should_be_able_to_fetch_a_stored_event()
        {
            var streamId = storeEvent("context", "someType", "someBody");
            var events = _storage.LoadEvents("context", streamId);

            Assert.AreEqual(1, events.Length);
            Assert.AreEqual(streamId, events[0].Stream);
            Assert.AreEqual("someType", events[0].TypeKey);
            Assert.AreEqual("someBody", events[0].Body);
        }

        [Test]
        public void should_be_able_to_fetch_a_collection_of_stored_events()
        {
            var commits = new CommitBuilder()
                .NewCommit(Guid.NewGuid(), "foo", "stream")
                .Event(new EventForStorage(Guid.NewGuid(), 1, DateTime.UtcNow, "someType", "someBody", null))
                .Event(new EventForStorage(Guid.NewGuid(), 2, DateTime.UtcNow, "someType", "someBody", null))
                .NewCommit(Guid.NewGuid(), "foo", "anotherStream")
                .Event(new EventForStorage(Guid.NewGuid(), 1, DateTime.UtcNow, "someType", "someBody", null))
                .Build();

            _storage.Store(commits);

            var events = _storage.LoadEvents("foo", "stream");

            Assert.AreEqual(2, events.Length, "stream should have two events");
            Assert.AreEqual("stream", events[0].Stream);
            Assert.AreEqual("someType", events[0].TypeKey);
            Assert.AreEqual("someBody", events[0].Body);

            var events2 = _storage.LoadEvents("foo", "anotherStream");

            Assert.AreEqual(1, events2.Length, "another stream should have 1 event.");
            Assert.AreEqual("anotherStream", events2[0].Stream);
            Assert.AreEqual("someType", events2[0].TypeKey);
            Assert.AreEqual("someBody", events2[0].Body);
        }

        [Test]
        public void commits_with_unexpected_starting_sequence_should_fail()
        {
            var commitId = Guid.NewGuid();
            var commit2Id = Guid.NewGuid();
            var commits = new CommitBuilder()
               .NewCommit(commitId, "foo", "stream")
               .Event(new EventForStorage(Guid.NewGuid(), 1, DateTime.UtcNow, "someType", "someBody", null))
               .Event(new EventForStorage(Guid.NewGuid(), 2, DateTime.UtcNow, "someType", "someBody", null))
               .NewCommit(commit2Id, "foo", "anotherStream")
               .Event(new EventForStorage(Guid.NewGuid(), 1, DateTime.UtcNow, "someType", "someBody", null))
               .Build();

            _storage.Store(commits);

            var commit3Id = Guid.NewGuid();
            var commit4Id = Guid.NewGuid();
            var newCommits = new CommitBuilder()
               .NewCommit(commit3Id, "foo", "stream")
               .Event(new EventForStorage(Guid.NewGuid(), 2, DateTime.UtcNow, "someType", "someBody", null))
               .Event(new EventForStorage(Guid.NewGuid(), 3, DateTime.UtcNow, "someType", "someBody", null))
               .NewCommit(commit4Id, "foo", "anotherStream")
               .Event(new EventForStorage(Guid.NewGuid(), 2, DateTime.UtcNow, "someType", "someBody", null))
               .Build();

            var results = _storage.Store(newCommits);
            Assert.AreEqual(1, results.SuccessfulCommits.Length);
            Assert.AreEqual(1, results.FailedDueToConcurrencyCommits.Length);
            Assert.AreEqual(commit4Id, results.SuccessfulCommits[0]);
            Assert.AreEqual(commit3Id, results.FailedDueToConcurrencyCommits[0]);

        }

        [Test]
        public void Successful_commits_should_be_stored_in_a_batch_of_successful_and_unsucessful_commits()
        {
            var commitId = Guid.NewGuid();
            var commit2Id = Guid.NewGuid();
            var commits = new CommitBuilder()
               .NewCommit(commitId, "foo", "stream")
               .Event(new EventForStorage(Guid.NewGuid(), 1, DateTime.UtcNow, "someType", "someBody", null))
               .Event(new EventForStorage(Guid.NewGuid(), 2, DateTime.UtcNow, "someType", "someBody", null))
               .NewCommit(commit2Id, "foo", "anotherStream")
               .Event(new EventForStorage(Guid.NewGuid(), 1, DateTime.UtcNow, "someType", "someBody", null))
               .Build();

            var results=_storage.Store(commits);
            var successfulCommitIds = new List<Guid>();
            successfulCommitIds.AddRange(results.SuccessfulCommits);
            var commit3Id = Guid.NewGuid();
            var commit4Id = Guid.NewGuid();
            var newCommits = new CommitBuilder()
               .NewCommit(commit3Id, "foo", "stream")
               .Event(new EventForStorage(Guid.NewGuid(), 2, DateTime.UtcNow, "someType", "someBody", null))
               .Event(new EventForStorage(Guid.NewGuid(), 3, DateTime.UtcNow, "someType", "someBody", null))
               .NewCommit(commit4Id, "foo", "anotherStream")
               .Event(new EventForStorage(Guid.NewGuid(), 2, DateTime.UtcNow, "someType", "someBody", null))
               .Build();

            var expectedListOfCommitIds = new List<Guid>();

            expectedListOfCommitIds.Add(commitId);
            expectedListOfCommitIds.Add(commit2Id);
            expectedListOfCommitIds.Add(commit4Id);

            results=_storage.Store(newCommits);
            successfulCommitIds.AddRange(results.SuccessfulCommits);

            var listOfStoredEvents = new List<EventInStorage>();

            var loadedEvents = _storage.LoadEvents("foo", "stream");
            listOfStoredEvents.AddRange(loadedEvents);
            loadedEvents = _storage.LoadEvents("foo", "anotherStream");
            listOfStoredEvents.AddRange(loadedEvents);
            
            var successfulResultCount = listOfStoredEvents.Count;


            Assert.AreEqual(4, successfulResultCount);
            Assert.That(successfulCommitIds, Is.SubsetOf(expectedListOfCommitIds));

        }

        [Test]
        public void loading_unstored_events_should_give_no_result()
        {
            var result=_storage.LoadEvents("foo", "stream");

            Assert.That(result,Is.Empty);
        }

        [Test]
        public void out_of_two_same_commits_the_one_with_lowest_timestamp_only_should_be_stored()
        {
            var commit1Id = Guid.NewGuid();
            var commit2Id = Guid.NewGuid();
            var commits = new CommitBuilder()
                .NewCommit(commit1Id, "foo", "stream")
                .Event(new EventForStorage(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(1), "someType", "someBody", null))
                .Event(new EventForStorage(Guid.NewGuid(), 2, DateTime.UtcNow.AddDays(1), "someType", "someBody", null))
                .NewCommit(commit2Id, "foo", "stream")
                .Event(new EventForStorage(Guid.NewGuid(), 1, DateTime.UtcNow, "someType", "someBody", null))
                .Event(new EventForStorage(Guid.NewGuid(), 2, DateTime.UtcNow, "someType", "someBody", null))
                .Build();



            var numberOfInitialCommits=commits.Commits.Length;

            Assert.AreEqual(2,numberOfInitialCommits);

            var result=_storage.Store(commits);
            

            var successfulCommit = result.SuccessfulCommits[0];
            Assert.That(commit2Id,Is.EqualTo(successfulCommit));

            var unsuccessfulCommit = result.FailedDueToConcurrencyCommits[0];
            Assert.That(commit1Id, Is.EqualTo(unsuccessfulCommit));
        }

        [Test]
        public void out_of_two_same_commits_the_one_comes_first_should_be_saved()
        {
            var commit1Id = Guid.NewGuid();
            var commit2Id = Guid.NewGuid();

            var timeStamp = DateTime.UtcNow;
            var commits = new CommitBuilder()
                .NewCommit(commit1Id, "foo", "stream")
                .Event(new EventForStorage(Guid.NewGuid(), 1, timeStamp, "someType", "someBody", null))
                .Event(new EventForStorage(Guid.NewGuid(), 2, timeStamp, "someType", "someBody", null))
                .NewCommit(commit2Id, "foo", "stream")
                .Event(new EventForStorage(Guid.NewGuid(), 1, timeStamp, "someType", "someBody", null))
                .Event(new EventForStorage(Guid.NewGuid(), 2, timeStamp, "someType", "someBody", null))
                .Build();

            var result = _storage.Store(commits);
            
            Assert.AreEqual(1, result.SuccessfulCommits.Length);
            Assert.AreEqual(1, result.FailedDueToConcurrencyCommits.Length);
        }

        [Test]
        public void commit_having_initial_sequence_0_should_not_be_saved()
        {
            var commits = new CommitBuilder()
                .NewCommit(Guid.NewGuid(), "foo", "stream")
                .Event(new EventForStorage(Guid.NewGuid(), 1, DateTime.UtcNow, "someType", "someBody", null))
                .Event(new EventForStorage(Guid.NewGuid(), 2, DateTime.UtcNow, "someType", "someBody", null))
                .NewCommit(Guid.NewGuid(), "foo", "AnotherStream")
                .Event(new EventForStorage(Guid.NewGuid(), 0, DateTime.UtcNow, "someType", "someBody", null))
                .Build();

            var result=_storage.Store(commits);

            Assert.AreEqual(1,result.SuccessfulCommits.Length);
            Assert.AreEqual(1,result.FailedDueToConcurrencyCommits.Length);
        }

        [Test]
        public void commits_with_concurrency_check_disabled_should_append()
        {
            var commits = new CommitBuilder()
                .NewCommit(Guid.NewGuid(), "foo", "stream")
                .Event(new EventForStorage(Guid.NewGuid(), -1, DateTime.UtcNow, "someType", "someBody", null))
                .Event(new EventForStorage(Guid.NewGuid(), -1, DateTime.UtcNow, "someType", "someBody", null))
                .NewCommit(Guid.NewGuid(), "foo", "AnotherStream")
                .Event(new EventForStorage(Guid.NewGuid(), -1, DateTime.UtcNow, "someType", "someBody", null))
                .Build();

            var result = _storage.Store(commits);

            Assert.AreEqual(2, result.SuccessfulCommits.Length);
        }

        [Test]
        public void commits_with_concurrency_check_disabled_should_append_when_there_are_existing_events()
        {
            var commits = new CommitBuilder()
                .NewCommit(Guid.NewGuid(), "foo", "stream")
                .Event(new EventForStorage(Guid.NewGuid(), 1, DateTime.UtcNow, "someType", "someBody", null))
                .Event(new EventForStorage(Guid.NewGuid(), 2, DateTime.UtcNow, "someType", "someBody", null))
                .Build();

            _storage.Store(commits);

            commits = new CommitBuilder()
                .NewCommit(Guid.NewGuid(), "foo", "stream")
                .Event(new EventForStorage(Guid.NewGuid(), -1, DateTime.UtcNow, "someNewType", "someBody1", null))
                .Event(new EventForStorage(Guid.NewGuid(), -1, DateTime.UtcNow, "someNewType2", "someBody2", null))
                .Build();

            var result = _storage.Store(commits);

            Assert.AreEqual(1, result.SuccessfulCommits.Length);
        }

        [Test]
        public void should_fetch_strored_events()
        {
            var event1Id = Guid.NewGuid();
            var event2Id = Guid.NewGuid();
            var event3Id = Guid.NewGuid();

            var commits = new CommitBuilder()
                .NewCommit(Guid.NewGuid(), "foo", "stream")
                .Event(new EventForStorage(event1Id, 1, DateTime.UtcNow, "someType", "someBody", null))
                .Event(new EventForStorage(event2Id, 2, DateTime.UtcNow, "someType", "someBody", null))
                .NewCommit(Guid.NewGuid(), "foo", "AnotherStream")
                .Event(new EventForStorage(event3Id, 1, DateTime.UtcNow, "someType", "someBody", null))
                .Build();

            _storage.Store(commits);

            var requests = new[]
                               {
                                   new FetchEventRequest(event1Id, "foo", "stream"),
                                   new FetchEventRequest(event2Id, "foo", "stream"),
                                   new FetchEventRequest(event3Id, "foo", "AnotherStream"),
                                   new FetchEventRequest(event1Id, "foo", "Missing")
                               };

            var results = _storage.FetchEvent(requests);

            Assert.AreEqual(event1Id, results[requests[0].RequestId].EventId);
            Assert.AreEqual(event2Id, results[requests[1].RequestId].EventId);
            Assert.AreEqual(event3Id, results[requests[2].RequestId].EventId);
            Assert.IsFalse(results.ContainsKey(requests[3].RequestId));
        }

        [Test]
        public void should_load_all_events_each_having_accurate_and_different_timeStamp()
        {
            var now = new DateTime(2013,1,1);
            var createEventsDetails = new Dictionary<Guid, CreateEventsMetaData>();
            var j = 0;
            for (var i = 1; i <= 50; i++)
            {
                createEventsDetails.Add(Guid.NewGuid(), new CreateEventsMetaData(2, "foo", "stream"+i, new DateTime[2] { now.AddMilliseconds(++j), now.AddMilliseconds(++j) }));
            }

            var commitDetails = new EventsInjector(_storage).InsertEvents(createEventsDetails);

            var listOfEvents = new List<EventInStorage>();

            for (int i = 1; i <= 50; i++)
            {
                listOfEvents.AddRange(_storage.LoadEvents("foo","stream"+i));
            }

            Assert.That(listOfEvents.Count,Is.EqualTo(100));
            var expectedListOfTimeStamp = new List<DateTime>();

            foreach (var c in commitDetails)
            {
                expectedListOfTimeStamp.AddRange(c.Events.Select(x => x.Timestamp).OrderBy(y=>y));
            }

            var listOfActualTimeStamp = listOfEvents.Select(eventInStorage => eventInStorage.Timestamp).OrderBy(y=>y).ToList();

            Assert.That(listOfActualTimeStamp,Is.EqualTo(expectedListOfTimeStamp),"The expected list of timestamp for all events are not equal to the actual list of timestamp when order by datetime Ticks ascending.");
        }


        private object storeEvent(string context, string typeKey, string body, string stream)
        {
            var @event = new EventForStorage(Guid.NewGuid(), 1, DateTime.UtcNow, typeKey, body, null);
            var commit = new CommitForStorage(Guid.NewGuid(), context, stream, @event);
            var commits = new CommitsForStorage(commit);
            _storage.Store(commits);

            return stream;
        }

        private object storeEvent(string context, string typeKey, string body)
        {
            return storeEvent(context, typeKey, body, Guid.NewGuid().ToString());
        }


        protected virtual void SetUpPerTest(EventStorage storage)
        {
        }

        [TestFixtureSetUp]
        public virtual void FixtureSetup()
        {
        }

        [TestFixtureTearDown]
        public virtual void FixtureTeardown()
        {
        }

        [SetUp]
        public void PerTestSetup()
        {
            _storage = GetStorage();
            SetUpPerTest(_storage);
        }

        [TearDown]
        public void PerTearDown()
        {
            TearDownPerTest(_storage);
        }

        protected virtual void TearDownPerTest(EventStorage storage)
        {
        }
    }
}