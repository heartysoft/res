using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Res.Client;
using Res.Client.Exceptions;
using Res.Client.Internal;
using Res.Core.Storage;
using Res.Protocol;
using EventInStorage = Res.Client.EventInStorage;
using QueueAlreadyExistsInContextWithDifferentFilterException = Res.Client.Exceptions.QueueAlreadyExistsInContextWithDifferentFilterException;

namespace Res.Core.Tests.Client
{
    [TestFixture]
    public abstract class TcpClientTests
    {
        protected ResHarness _harness;

        [TestFixtureSetUp]
        public void Setup()
        {
            FixtureSetup();
        }

        protected abstract void FixtureSetup();
        protected abstract void FixtureTeardown();
        protected abstract void PerTestInit();
        protected abstract void PerTestCleanup();


        [TestFixtureTearDown]
        public void Teardown()
        {
            FixtureTeardown();
        }

        [SetUp]
        public void PerTestSetup()
        {
            PerTestInit();
        }

        [TearDown]
        public void PerTestTeardown()
        {
            PerTestCleanup();
        }


        [Test]
        public void ShouldBootUpAndTeardown()
        {
            var client = _harness.CreatePublisher();
        }

        [Test]
        public void ShouldStoreAnEvent()
        {
            var client = _harness.CreatePublisher();
            var commit = client.CommitAsync("test-context", Guid.NewGuid().ToString(), new[]
            {
                new EventData("test", Guid.NewGuid(), "", "some body", DateTime.Now) 
            }, ExpectedVersion.OnlyNew, TimeSpan.FromSeconds(1));

            commit.Wait();
        }

        [Test]
        public void ShouldStoreMultiplEvents()
        {
            var client = _harness.CreatePublisher();
            var commit = client.CommitAsync("test-context", Guid.NewGuid().ToString(), new[]
            {
                new EventData("test", Guid.NewGuid(), "", "some body", DateTime.Now),
                new EventData("test1", Guid.NewGuid(), "", "something more", DateTime.Now),
                new EventData("test", Guid.NewGuid(), "", "a bit more", DateTime.Now) 
            }, ExpectedVersion.OnlyNew);

            commit.Wait();
        }


        [Test]
        public void ShouldHandleMultipleNonConflictingCommits()
        {
            var client = _harness.CreatePublisher();
            var commit = client.CommitAsync("test-context", Guid.NewGuid().ToString(), new[]
            {
                new EventData("test", Guid.NewGuid(), "", "some body", DateTime.Now),
                new EventData("test1", Guid.NewGuid(), "", "something more", DateTime.Now),
                new EventData("test", Guid.NewGuid(), "", "a bit more", DateTime.Now) 
            }, ExpectedVersion.OnlyNew);

            var commit2 = client.CommitAsync("test-context", Guid.NewGuid().ToString(), new[]
            {
                new EventData("test", Guid.NewGuid(), "", "some body", DateTime.Now),
                new EventData("test1", Guid.NewGuid(), "", "something more", DateTime.Now),
                new EventData("test", Guid.NewGuid(), "", "a bit more", DateTime.Now) 
            }, ExpectedVersion.OnlyNew);

            Task.WhenAll(commit, commit2).Wait();
        }

        [Test]
        public void ShouldCommitRawEventWithNullHeader()
        {
            var client = _harness.CreatePublisher();
            var commit = client.CommitAsync("test-context", Guid.NewGuid().ToString(), new[]
            {
                new EventData("test", Guid.NewGuid(), null, "some body", DateTime.Now),
                new EventData("test1", Guid.NewGuid(), "", "something more", DateTime.Now),
                new EventData("test", Guid.NewGuid(), null, "a bit more", DateTime.Now) 
            }, ExpectedVersion.OnlyNew);


            var task = Task.WhenAny(commit, Task.Delay(2000)).GetAwaiter().GetResult();

            Assert.AreEqual(commit, task);
        }

        [Test]
        public void ConflictingWritesShouldFail()
        {
            var client = _harness.CreatePublisher();
            var stream = Guid.NewGuid().ToString();

            var commit = client.CommitAsync("test-context", stream, new[]
            {
                new EventData("test", Guid.NewGuid(), "", "some body", DateTime.Now),
                new EventData("test1", Guid.NewGuid(), "", "something more", DateTime.Now),
                new EventData("test", Guid.NewGuid(), "", "a bit more", DateTime.Now) 
            }, ExpectedVersion.OnlyNew);

            var commit2 = client.CommitAsync("test-context", stream, new[]
            {
                new EventData("test", Guid.NewGuid(), "", "some body", DateTime.Now),
                new EventData("test1", Guid.NewGuid(), "", "something more", DateTime.Now),
                new EventData("test", Guid.NewGuid(), "", "a bit more", DateTime.Now) 
            }, ExpectedVersion.OnlyNew);

            try
            {
                Task.WaitAll(commit, commit2);
            }
            catch (AggregateException e)
            {
                Assert.AreEqual(1, e.InnerExceptions.Count);
                Assert.IsInstanceOf<ConcurrencyException>(e.InnerException);
            }
        }

        [Test]
        public void ShouldHandleSerialisedWrites()
        {
            var client = _harness.CreatePublisher();
            var commit = client.CommitAsync("test-context", Guid.NewGuid().ToString(), new[]
            {
                new EventData("test", Guid.NewGuid(), "", "some body", DateTime.Now),
                new EventData("test1", Guid.NewGuid(), "", "something more", DateTime.Now),
                new EventData("test", Guid.NewGuid(), "", "a bit more", DateTime.Now) 
            }, ExpectedVersion.OnlyNew);

            commit.Wait();

            var commit2 = client.CommitAsync("test-context", Guid.NewGuid().ToString(), new[]
            {
                new EventData("test", Guid.NewGuid(), "", "some body", DateTime.Now),
                new EventData("test1", Guid.NewGuid(), "", "something more", DateTime.Now),
                new EventData("test", Guid.NewGuid(), "", "a bit more", DateTime.Now) 
            }, ExpectedVersion.OnlyNew);

            commit2.Wait();
        }

        [Test]
        public void ShouldLodEventsByStream()
        {
            var publisher = _harness.CreatePublisher();
            var query = _harness.CreateQueryClient();

            var stream = Guid.NewGuid().ToString();

            var event1Id = Guid.NewGuid();
            var event2Id = Guid.NewGuid();
            var event3Id = Guid.NewGuid();

            var commit = publisher.CommitAsync("test-context", stream, new[]
            {
                new EventData("test", event1Id, "", "some body", DateTime.Now),
                new EventData("test1", event2Id, "", "something more", DateTime.Now),
                new EventData("test", event3Id, "", "a bit more", DateTime.Now) 
            }, ExpectedVersion.OnlyNew);

            commit.Wait();

            var events = query.LoadEvents("test-context", stream, 0, null, null)
                .Result;

            Assert.AreEqual("test-context", events.Context);
            Assert.AreEqual(stream, events.Stream);
            Assert.AreEqual(3, events.Events.Length);
            Assert.AreEqual(event1Id, events.Events[0].EventId);
            Assert.AreEqual(event2Id, events.Events[1].EventId);
            Assert.AreEqual(event3Id, events.Events[2].EventId);
        }

        [Test]
        public void ShouldGetQueuedMessages()
        {
            var publisher = _harness.CreatePublisher();
            var queueEngine = _harness.QueueEngine;

            var stream = Guid.NewGuid().ToString();

            var event1Id = Guid.NewGuid();
            var event2Id = Guid.NewGuid();
            var event3Id = Guid.NewGuid();
            var event4Id = Guid.NewGuid();
            var event5Id = Guid.NewGuid();


            var commit = publisher.CommitAsync("test-context", stream, new[]
            {
                new EventData("test", event1Id, "", "some body", DateTime.Now),
                new EventData("test1", event2Id, "", "something more", DateTime.Now),
                new EventData("test", event3Id, "", "a bit more", DateTime.Now), 
                new EventData("test", event4Id, "", "a bit more", DateTime.Now), 
                new EventData("test", event5Id, "", "a bit more", DateTime.Now) 
            }, ExpectedVersion.OnlyNew);

            commit.Wait();

            var queue1 = queueEngine.Declare("test-context", "test-queue", "queue1", "*", DateTime.Now.AddDays(-1));
            var queue1Events = queue1.Next(2, TimeSpan.FromDays(1), TimeSpan.FromSeconds(10)).Result;

            var queue2 = queueEngine.Declare("test-context", "test-queue", "queue2", "*", DateTime.Now.AddDays(-1));
            var queue2Events = queue2.Next(1, TimeSpan.FromDays(1), TimeSpan.FromSeconds(10)).Result;

            Assert.AreEqual(2, queue1Events.Events.Length);
            Assert.AreEqual(1, queue2Events.Events.Length);

            Assert.AreEqual(event1Id, queue1Events.Events[0].EventId);
            Assert.AreEqual(event2Id, queue1Events.Events[1].EventId);

            Assert.AreEqual(event3Id, queue2Events.Events[0].EventId);

            queue1Events = queue1.Next(1, TimeSpan.FromDays(1), TimeSpan.FromSeconds(10)).Result;
            queue2Events = queue2.Next(1, TimeSpan.FromDays(1), TimeSpan.FromSeconds(10)).Result;

            Assert.AreEqual(1, queue1Events.Events.Length);
            Assert.AreEqual(1, queue2Events.Events.Length);

            Assert.AreEqual(event4Id, queue1Events.Events[0].EventId);
            Assert.AreEqual(event5Id, queue2Events.Events[0].EventId);

            queue1Events = queue1.Next(1, TimeSpan.FromDays(1), TimeSpan.FromSeconds(10)).Result;
            queue2Events = queue2.Next(1, TimeSpan.FromDays(1), TimeSpan.FromSeconds(10)).Result;

            Assert.AreEqual(0, queue1Events.Events.Length);
            Assert.AreEqual(0, queue2Events.Events.Length);

            var event6Id = Guid.NewGuid();
            var commit2 = publisher.CommitAsync("test-context", stream, new[]
            {
                new EventData("test", event6Id, "", "some body", DateTime.Now),
            }, 6);

            commit2.Wait();

            queue1Events = queue1.Next(1, TimeSpan.FromDays(1), TimeSpan.FromSeconds(10)).Result;
            queue2Events = queue2.Next(1, TimeSpan.FromDays(1), TimeSpan.FromSeconds(10)).Result;

            Assert.AreEqual(1, queue1Events.Events.Length);
            Assert.AreEqual(0, queue2Events.Events.Length);

            Assert.AreEqual(event6Id, queue1Events.Events[0].EventId);
        }

        [Test]
        [ExpectedException(typeof(QueueAlreadyExistsInContextWithDifferentFilterException))]
        public async Task attempting_to_create_queue_with_same_name_in_context_should_fail_if_filter_is_different()
        {
            var publisher = _harness.CreatePublisher();
            var queueEngine = _harness.QueueEngine;

            var stream = Guid.NewGuid().ToString();

            var event1Id = Guid.NewGuid();
            var event2Id = Guid.NewGuid();
            var event3Id = Guid.NewGuid();
            var event4Id = Guid.NewGuid();
            var event5Id = Guid.NewGuid();


            var commit = publisher.CommitAsync("test-context", stream, new[]
            {
                new EventData("test", event1Id, "", "some body", DateTime.Now),
                new EventData("test1", event2Id, "", "something more", DateTime.Now),
                new EventData("test", event3Id, "", "a bit more", DateTime.Now),
                new EventData("test", event4Id, "", "a bit more", DateTime.Now),
                new EventData("test", event5Id, "", "a bit more", DateTime.Now)
            }, ExpectedVersion.OnlyNew);

            commit.Wait();

            var queue1 = queueEngine.Declare("test-context", "test-queue", "queue1", "*", DateTime.Now.AddDays(-1));
            var queue2 = queueEngine.Declare("test-context", "test-queue", "queue1", "test-", DateTime.Now.AddDays(-1));

            var queue1Events = await queue1.Next(1, TimeSpan.FromDays(1), TimeSpan.FromSeconds(1000));
            var queue2Events = await queue2.Next(1, TimeSpan.FromDays(1), TimeSpan.FromSeconds(1000));
        }

        [Test]
        public async Task attempting_to_create_queue_with_same_name_in_context_should_succeed_if_filter_is_same()
        {
            var publisher = _harness.CreatePublisher();
            var queueEngine = _harness.QueueEngine;

            var stream = Guid.NewGuid().ToString();

            var event1Id = Guid.NewGuid();
            var event2Id = Guid.NewGuid();
            var event3Id = Guid.NewGuid();
            var event4Id = Guid.NewGuid();
            var event5Id = Guid.NewGuid();


            var commit = publisher.CommitAsync("test-context", stream, new[]
            {
                new EventData("test", event1Id, "", "some body", DateTime.Now),
                new EventData("test1", event2Id, "", "something more", DateTime.Now),
                new EventData("test", event3Id, "", "a bit more", DateTime.Now),
                new EventData("test", event4Id, "", "a bit more", DateTime.Now),
                new EventData("test", event5Id, "", "a bit more", DateTime.Now)
            }, ExpectedVersion.OnlyNew);

            commit.Wait();

            var queue1 = queueEngine.Declare("test-context", "test-queue", "queue1", "test-", DateTime.Now.AddDays(-1));
            var queue2 = queueEngine.Declare("test-context", "test-queue", "queue1", "test-", DateTime.Now.AddDays(-1));

            var queue1Events = await queue1.Next(1, TimeSpan.FromDays(1), TimeSpan.FromSeconds(1000));
            var queue2Events = await queue2.Next(1, TimeSpan.FromDays(1), TimeSpan.FromSeconds(1000));
        }
    }
}