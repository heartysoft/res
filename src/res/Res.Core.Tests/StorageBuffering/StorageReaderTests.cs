using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Res.Core.Storage;
using Res.Core.StorageBuffering;
using Res.Core.Tests.Storage;

namespace Res.Core.Tests.StorageBuffering
{
    [TestFixture]
    public class StorageReaderTests
    {
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

        [Test]
        public void Should_load_nothing_when_there_is_no_event()
        {
            var storage = new InMemoryEventStorage();
            var reader = new EventStorageReader(2000, TimeSpan.FromMinutes(5), storage);

            var events = reader.LoadEventsForStream("foo", "stream");

            Assert.That(events.Length, Is.EqualTo(0), "events list should be empty as there is no events to laod!");
        }

        [Test]
        [ExpectedException(typeof(EventStorageReader.StorageReaderBusyException))]
        public void should_throw__reader_busy_exception_when_the_reader_max_count_is_reached()
        {
            var storage = new InMemoryEventStorage();

            var now = new DateTime(2013, 1, 1);
            var createEventsDetails = new Dictionary<Guid, CreateEventsMetaData>();
            var j = 0;
            for (var i = 1; i <= 6; i++)
            {
                createEventsDetails.Add(Guid.NewGuid(), new CreateEventsMetaData(2, "foo", "stream", new[] { now.AddMilliseconds(++j), now.AddMilliseconds(++j) }));
            }

            var commitDetails = new EventsInjector(storage).InsertEvents(createEventsDetails);
            var reader = new EventStorageReader(10, TimeSpan.FromMinutes(5), storage);
            var listOfTasksOfEvents = new List<Task>();

            for (int i = 1; i < 12; i++)
            {
                var eventTask = reader.Fetch(new FetchEventRequest(commitDetails[0].Events[0].EventId, "foo", "stream"));
                listOfTasksOfEvents.Add(eventTask);
            }

        }

        [Test]
        public void should_be_able_to_fetch_a_particular_event()
        {
            var storage = new InMemoryEventStorage();

            var now = new DateTime(2013, 1, 1);
            var createEventsDetails = new Dictionary<Guid, CreateEventsMetaData>();
            var j = 0;
            for (var i = 1; i <= 1; i++)
            {
                createEventsDetails.Add(Guid.NewGuid(), new CreateEventsMetaData(1, "foo", "stream", new[] { now.AddMilliseconds(++j) }));
            }
            var commitDetails = new EventsInjector(storage).InsertEvents(createEventsDetails);
            var reader = new EventStorageReader(10, TimeSpan.FromMinutes(5), storage);
            var eventTask = reader.Fetch(new FetchEventRequest(commitDetails[0].Events[0].EventId, "foo", "stream"));
            reader.Start(_token.Token);
            var @event = eventTask.Result;
            var expecetdEvent = commitDetails[0].Events[0];

            Assert.That(@event, !Is.Null, "No event is fetched");
            Assert.That(@event.EventId, Is.EqualTo(expecetdEvent.EventId), "This is not the exepected event from the fetch opeartion");
            Assert.That(@event.Sequence, Is.EqualTo(expecetdEvent.Sequence), "This is not the exepected sequence of the event from the fetch opeartion");
            Assert.That(@event.Timestamp, Is.EqualTo(expecetdEvent.Timestamp), "This is not the exepected sequence of the event from the fetch opeartion");
            Assert.That(@event.TypeKey, Is.EqualTo(expecetdEvent.TypeKey), "This is not the exepected type of the event from the fetch opeartion");

        }

        [Test]
        public void should_be_able_to_load_events_for_a_context_and_a_stream()
        {
            var storage = new InMemoryEventStorage();

            var now = new DateTime(2013, 1, 1);
            var createEventsDetails = new Dictionary<Guid, CreateEventsMetaData>();
            var j = 0;
            for (var i = 1; i <= 6; i++)
            {
                createEventsDetails.Add(Guid.NewGuid(), new CreateEventsMetaData(2, "foo", "stream" + i, new[] { now.AddMilliseconds(++j), now.AddMilliseconds(++j) }));
            }
            var commitDetails = new EventsInjector(storage).InsertEvents(createEventsDetails);
            var reader = new EventStorageReader(10, TimeSpan.FromMinutes(5), storage);
            for (var i = 1; i <= 6; i++)
            {
                var events = reader.LoadEventsForStream("foo", "stream" + i);
                Assert.That(events.Length, Is.EqualTo(2));
            }

        }

        [Test]
        [ExpectedException(typeof(EventStorageReader.StorageReaderTimeoutException))]
        public void should_drop_fetch_when_timeout()
        {
            var storage = new InMemoryEventStorage();

            var now = new DateTime(2013, 1, 1);
            var createEventsDetails = new Dictionary<Guid, CreateEventsMetaData>();
            var j = 0;
            for (var i = 1; i <= 6; i++)
            {
                createEventsDetails.Add(Guid.NewGuid(), new CreateEventsMetaData(2, "foo", "stream" + i, new[] { now.AddMilliseconds(++j), now.AddMilliseconds(++j) }));
            }
            var commitDetails = new EventsInjector(storage).InsertEvents(createEventsDetails);
            var reader = new EventStorageReader(10, TimeSpan.FromMilliseconds(10), storage);

            var @event = reader.Fetch(new FetchEventRequest(commitDetails[0].Events[0].EventId, "foo", "stream1"));

            Task.Delay(10).Wait();
            reader.Start(_token.Token);

            try
            {
                var result = @event.Result;
            }
            catch (AggregateException ae)
            {

                throw ae.InnerException;
            }

        }

        [Test]
        [ExpectedException(typeof(EventStorageReader.EventNotFoundException))]
        public void should_cascade_event_not_found_exception_for_invalid_event()
        {
            var storage = new InMemoryEventStorage();

            var now = new DateTime(2013, 1, 1);
            var createEventsDetails = new Dictionary<Guid, CreateEventsMetaData>();
            var j = 0;
            for (var i = 1; i <= 6; i++)
            {
                createEventsDetails.Add(Guid.NewGuid(), new CreateEventsMetaData(2, "foo", "stream" + i, new[] { now.AddMilliseconds(++j), now.AddMilliseconds(++j) }));
            }
            var commitDetails = new EventsInjector(storage).InsertEvents(createEventsDetails);
            var reader = new EventStorageReader(10, TimeSpan.FromMilliseconds(10), storage);

            var @event = reader.Fetch(new FetchEventRequest(commitDetails[0].Events[0].EventId, "foo", "stream"));

            reader.Start(_token.Token);

            try
            {
                var result = @event.Result;
            }
            catch (AggregateException ae)
            {

                throw ae.InnerException;
            }
        }

        [Test]
        public void there_should_be_exactly_one_read_per_batch()
        {
            var storage = new InMemoryEventStorage();

            var now = new DateTime(2013, 1, 1);
            var createEventsDetails = new Dictionary<Guid, CreateEventsMetaData>();
            var j = 0;
            for (var i = 1; i <= 6; i++)
            {
                createEventsDetails.Add(Guid.NewGuid(), new CreateEventsMetaData(2, "foo", "stream" + i, new[] { now.AddMilliseconds(++j), now.AddMilliseconds(++j) }));
            }
            var commitDetails = new EventsInjector(storage).InsertEvents(createEventsDetails);
            var reader = new EventStorageReader(100, TimeSpan.FromSeconds(10), storage, 2);

            var listOfTasksOfEvents = new List<Task>();

            for (int i = 0; i < 6; i++)
            {
                for (int k = 0; k < 2; k++)
                {
                    var eventTask = reader.Fetch(new FetchEventRequest(commitDetails[i].Events[k].EventId, "foo", "stream" + (i + 1)));
                    listOfTasksOfEvents.Add(eventTask);
                }

            }

            reader.Start(_token.Token);

            try
            {
                Task.WaitAll(listOfTasksOfEvents.ToArray());
                Assert.That(storage.ReadCount, Is.EqualTo(6));
            }
            catch (AggregateException ae)
            {

                throw ae.InnerException;
            }
        }


    }
}