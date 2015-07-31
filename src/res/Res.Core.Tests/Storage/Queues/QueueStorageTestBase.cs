using System;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using Res.Core.Storage;

namespace Res.Core.Tests.Storage.Queues
{
    [TestFixture]
    public abstract class QueueStorageTestBase
    {
        private QueueStorage _queueStorage;
        private EventStorage _eventStorage;
        protected abstract QueueStorage GetQueueStorage();
        protected abstract EventStorage GetEventStorage();

        protected virtual void SetUpPerTest(QueueStorage storage, EventStorage eventStorage)
        {
        }

        [SetUp]
        public void SetUp()
        {
            _eventStorage = GetEventStorage();
            _queueStorage = GetQueueStorage();

            SetUpPerTest(_queueStorage, _eventStorage);
        }

        [Test]
        public void should_create_queue_for_no_events()
        {
            _queueStorage.Subscribe(new SubscribeToQueue("foo", "bar", "test", "*", DateTime.UtcNow, 100, 60000));

            var queues = _queueStorage.GetAllByDecreasingNextMarker(10, 0);
            
            Assert.AreEqual(1, queues.Length);
            Assert.AreEqual("foo", queues[0].QueueId);
            Assert.AreEqual("test", queues[0].Context);
            Assert.AreEqual("*", queues[0].Filter);
            Assert.AreEqual(long.MinValue, queues[0].NextMarker);
        }

        [Test]
        public void should_not_create_allocation_for_no_events()
        {
            var queued = _queueStorage.Subscribe(new SubscribeToQueue("foo", "bar", "test", "*", DateTime.UtcNow, 100, 60000));
            Assert.IsNull(queued.AllocationId);
        }

        [Test]
        public void should_create_allocation_for_events()
        {
            _eventStorage.Store(new CommitsForStorage(new CommitForStorage(
                Guid.NewGuid(),
                "test",
                "bar",
                new EventForStorage(Guid.NewGuid(), 1, DateTime.UtcNow, "some-type", "body", "")
                )));

            var queued = _queueStorage.Subscribe(new SubscribeToQueue("foo", "bar", "test", "*", DateTime.UtcNow.AddMinutes(-5), 100, 60000));

            Assert.IsTrue(queued.AllocationId.HasValue);
            Assert.AreEqual("test", queued.Events[0].Context);
        }

        [Test]
        public void should_only_fetch_events_in_allocation()
        {
            var event1Id = Guid.NewGuid();
            var event2Id = Guid.NewGuid();

            _eventStorage.Store(new CommitsForStorage(new CommitForStorage(
                Guid.NewGuid(),
                "test",
                "bar",
                new EventForStorage(event1Id, 1, DateTime.UtcNow, "some-type", "body", ""),
                new EventForStorage(event2Id, 2, DateTime.UtcNow, "some-type", "body", "")
                )));

            var queued = _queueStorage.Subscribe(new SubscribeToQueue("foo", "bar", "test", "*", DateTime.UtcNow.AddMinutes(-5), 1, 60000));

            Assert.AreEqual(1, queued.Events.Length);
            Assert.AreEqual(event1Id, queued.Events[0].EventId);
        }

        [Test]
        public void should_return_same_allocation_if_not_expired()
        {
            var event1Id = Guid.NewGuid();
            var event2Id = Guid.NewGuid();

            _eventStorage.Store(new CommitsForStorage(new CommitForStorage(
                Guid.NewGuid(),
                "test",
                "bar",
                new EventForStorage(event1Id, 1, DateTime.UtcNow, "some-type", "body", ""),
                new EventForStorage(event2Id, 2, DateTime.UtcNow, "some-type", "body", "")
                )));

            var queued = _queueStorage.Subscribe(new SubscribeToQueue("foo", "bar", "test", "*", DateTime.UtcNow.AddMinutes(-5), 2, 60000));
            var queued2 = _queueStorage.Subscribe(new SubscribeToQueue("foo", "bar", "test", "*", DateTime.UtcNow.AddMinutes(-5), 2, 60000));

            Assert.AreEqual(queued.AllocationId.Value, queued2.AllocationId.Value);
            Assert.AreEqual(queued.Events[0].EventId, queued2.Events[0].EventId);
            Assert.AreEqual(queued.Events[1].EventId, queued2.Events[1].EventId);
        }

        [Test]
        public void should_reallocate_if_expired_allocation_for_queue_is_present()
        {
            var event1Id = Guid.NewGuid();

            _eventStorage.Store(new CommitsForStorage(new CommitForStorage(
                Guid.NewGuid(),
                "test",
                "bar",
                new EventForStorage(event1Id, 1, DateTime.UtcNow, "some-type", "body", "")
                )));

            var queued = _queueStorage.Subscribe(new SubscribeToQueue("foo", "baz", "test", "*", DateTime.UtcNow.AddMinutes(-5), 1, 1));
            var allocationId = queued.AllocationId.Value;
            
            Thread.Sleep(5);
            queued = _queueStorage.Subscribe(new SubscribeToQueue("foo", "bar", "test", "*", DateTime.UtcNow.AddMinutes(-5), 1, 1));

            Assert.AreEqual(allocationId, queued.AllocationId.Value);
        }

        [Test]
        public void should_create_allocation_if_there_are_allocations_to_other_subscribers_but_none_have_expired()
        {
            var event1Id = Guid.NewGuid();
            var event2Id = Guid.NewGuid();

            _eventStorage.Store(new CommitsForStorage(new CommitForStorage(
                Guid.NewGuid(),
                "test",
                "bar",
                new EventForStorage(event1Id, 1, DateTime.UtcNow, "some-type", "body", ""),
                new EventForStorage(event2Id, 2, DateTime.UtcNow, "some-type", "body", "")
                )));

            var queued = _queueStorage.Subscribe(new SubscribeToQueue("foo", "baz", "test", "*", DateTime.UtcNow.AddMinutes(-5), 1, 60000));
            var queued2 = _queueStorage.Subscribe(new SubscribeToQueue("foo", "bar", "test", "*", DateTime.UtcNow.AddMinutes(-5), 1, 60000));

            Assert.AreNotEqual(queued.AllocationId.Value, queued2.AllocationId.Value);
        }

        [Test]
        public void should_only_get_events_matching_filter()
        {
            var event1Id = Guid.NewGuid();
            var event2Id = Guid.NewGuid();

            _eventStorage.Store(new CommitsForStorage(new CommitForStorage(
                Guid.NewGuid(),
                "test",
                "bar",
                new EventForStorage(event1Id, 1, DateTime.UtcNow, "some-type", "body", "")
                )));

            _eventStorage.Store(new CommitsForStorage(new CommitForStorage(
                Guid.NewGuid(),
                "test",
                "baz",
                new EventForStorage(event2Id, 1, DateTime.UtcNow, "some-type", "body", "")
                )));

            var queued = _queueStorage.Subscribe(new SubscribeToQueue("foo", "bazSub", "test", "baz", DateTime.UtcNow.AddMinutes(-5), 10, 60000));

            Assert.AreEqual(1, queued.Events.Length);
            Assert.AreEqual(event2Id, queued.Events[0].EventId);

            var queues = _queueStorage.GetAllByDecreasingNextMarker(1, 0);
            Assert.AreEqual(queued.Events[0].GlobalSequence+1, queues[0].NextMarker);
        }

        [Test]
        public void should_proceed_to_next_events_after_acknowledgement()
        {
            var event1Id = Guid.NewGuid();
            var event2Id = Guid.NewGuid();

            _eventStorage.Store(new CommitsForStorage(new CommitForStorage(
                Guid.NewGuid(),
                "test",
                "bar",
                new EventForStorage(event1Id, 1, DateTime.UtcNow, "some-type", "body", ""),
                new EventForStorage(event2Id, 2, DateTime.UtcNow, "some-type", "body", "")
                )));

            var queued = _queueStorage.Subscribe(new SubscribeToQueue("foo", "bar", "test", "*", DateTime.UtcNow.AddMinutes(-5), 1, 60000));

            Assert.AreEqual(event1Id, queued.Events[0].EventId);

            var next =
                _queueStorage.AcknowledgeAndFetchNext(new AcknowledgeQueue("foo", "bar", queued.AllocationId, 1, 5000));

            Assert.AreEqual(event2Id, next.Events[0].EventId);
        }
    }
}