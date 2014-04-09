using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using NUnit.Framework;
using Res.Core.Storage;
using Res.Core.StorageBuffering;
using Res.Core.Tests.Storage;

namespace Res.Core.Tests.StorageBuffering
{
    [TestFixture]
    public class StorageWriterTests
    {
        [Test]
        [ExpectedException(typeof(EventStorageWriter.StorageWriterBusyException))]
        public void should_throw_if_busy()
        {
            var writer = new EventStorageWriter(1, TimeSpan.FromMinutes(1), new InMemoryEventStorage());
            writer.Store(new CommitForStorage("foo", "stream1",
                                              new EventForStorage(Guid.NewGuid(), 1, DateTime.Now, "type", "body", null)));
            writer.Store(new CommitForStorage("foo", "stream2",
                                              new EventForStorage(Guid.NewGuid(), 1, DateTime.Now, "type", "body", null)));
        }

        [Test]
        public void should_store_messages()
        {
            var storage = new InMemoryEventStorage();
            var writer = new EventStorageWriter(10, TimeSpan.FromMinutes(1), storage);

            writer.Start(_token.Token);

            var task1 = writer.Store(new CommitForStorage("foo", "stream1",
                                              new EventForStorage(Guid.NewGuid(), 1, DateTime.Now, "type", "body", null)));
            var task2 = writer.Store(new CommitForStorage("foo", "stream2",
                                              new EventForStorage(Guid.NewGuid(), 1, DateTime.Now, "type", "body", null)));

            Task.WhenAll(task1, task2).Wait(1000);

            var stream1Events = storage.LoadEvents("foo", "stream1");
            var stream2Events = storage.LoadEvents("foo", "stream2");

            Assert.AreEqual(1, stream1Events.Length, "stream1 should have one event.");
            Assert.AreEqual(1, stream2Events.Length, "stream2 should have one event.");
        }

        [Test]
        public void messages_for_one_batch_should_have_one_write()
        {
            var storage = new InMemoryEventStorage();
            var writer = new EventStorageWriter(10, TimeSpan.FromMinutes(1), storage);


            var task1 = writer.Store(new CommitForStorage("foo", "stream1",
                                              new EventForStorage(Guid.NewGuid(), 1, DateTime.Now, "type", "body", null)));
            var task2 = writer.Store(new CommitForStorage("foo", "stream2",
                                              new EventForStorage(Guid.NewGuid(), 1, DateTime.Now, "type", "body", null)));
            writer.Start(_token.Token);

            Task.WhenAll(task1, task2).Wait(1000);

            Assert.AreEqual(1, storage.WriteCount);
        }

        [Test]
        public void messages_for_different_batched_should_have_different_writes()
        {
            var storage = new InMemoryEventStorage();
            var writer = new EventStorageWriter(10, TimeSpan.FromMinutes(1), storage, 2);

            var task1 = writer.Store(new CommitForStorage("foo", "stream1",
                                              new EventForStorage(Guid.NewGuid(), 1, DateTime.Now, "type", "body", null)));
            var task2 = writer.Store(new CommitForStorage("foo", "stream2",
                                              new EventForStorage(Guid.NewGuid(), 1, DateTime.Now, "type", "body", null)));
            var task3 = writer.Store(new CommitForStorage("foo", "stream1",
                                              new EventForStorage(Guid.NewGuid(), 2, DateTime.Now, "type", "body", null)));
            var task4 = writer.Store(new CommitForStorage("foo", "stream2",
                                              new EventForStorage(Guid.NewGuid(), 2, DateTime.Now, "type", "body", null)));
            writer.Start(_token.Token);

            Task.WhenAll(task1, task2, task3, task4).Wait(1000);

            Assert.AreEqual(2, storage.WriteCount);
        }

        [Test]
        [ExpectedException(typeof(EventStorageException))]
        public void storage_exception_should_get_sent_to_task()
        {
            var storage = A.Fake<EventStorage>();
            A.CallTo(storage).Throws(() => new EventStorageException("Damn", null));
 
            var writer = new EventStorageWriter(10, TimeSpan.FromMinutes(1), storage, 2);

            var task1 = writer.Store(new CommitForStorage("foo", "stream1",
                                              new EventForStorage(Guid.NewGuid(), 1, DateTime.Now, "type", "body", null)));
            writer.Start(_token.Token);

            try
            {
                task1.Wait(1000);
            }
            catch (AggregateException e)
            {
                throw e.Flatten().InnerException;
            }
        }

        [Test]
        [ExpectedException(typeof(EventStorageWriter.StorageWriterTimeoutException))]
        public void should_drop_commits_older_than_timeout()
        {
            var storage = new InMemoryEventStorage();
            var writer = new EventStorageWriter(10, TimeSpan.FromMilliseconds(1), storage, 2);

            var task1 = writer.Store(new CommitForStorage("foo", "stream1",
                                              new EventForStorage(Guid.NewGuid(), 1, DateTime.Now, "type", "body", null)));
            var task2 = writer.Store(new CommitForStorage("foo", "stream2",
                                              new EventForStorage(Guid.NewGuid(), 1, DateTime.Now, "type", "body", null)));

            Task.Delay(1).Wait();

            writer.Start(_token.Token);

            try
            {
                Task.WhenAll(task1, task2).Wait(1000);
            }
            catch (AggregateException e)
            {
                throw e.Flatten().InnerException;
            }
        }

        [Test]
        //Make sure logging is off for this one....otherwise....SLOW.
        public void should_be_able_to_write_max_commits_in_one_batch()
        {
            var storage = new InMemoryEventStorage();
            var writer = new EventStorageWriter(3000, TimeSpan.FromMinutes(5), storage,3000);
            var listOfTasks = new List<Task>();

            for(var i=0;i<3000;i++)
            {
                listOfTasks.Add(writer.Store(new CommitForStorage("foo", "stream1",
                                              new EventForStorage(Guid.NewGuid(), i+1, DateTime.Now, "type", "body", null))));
            }

            
            writer.Start(_token.Token);

            Task.WhenAll(listOfTasks).Wait();

            Assert.That(storage.WriteCount,Is.EqualTo(1),"The commits are written in multiple batches.");

        }



        private CancellationTokenSource _token;

        [SetUp]
        public void Setup()
        {
            _token = new CancellationTokenSource();
        }

        [TearDown]
        public void Teardown()
        {
            _token.Cancel();
        }
    }
}