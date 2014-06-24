using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
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

namespace Res.Core.Tests.Client
{
    [TestFixture]
    public class TcpClientTests
    {
        private ResHarness _harness;

        [TestFixtureSetUp]
        public void Setup()
        {
            _harness = new ResHarness();
            _harness.Start();
        }

        [TestFixtureTearDown]
        public void Teardown()
        {
            _harness.Stop();
        }

        [SetUp]
        public void PerTestSetup()
        {
            //_harness = new ResHarness();
            //_harness.Start();
            ClearResStore();
        }

        [TearDown]
        public void PerTestTeardown()
        {
            //_harness.Stop();
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

        //[Test]
        //public void ShouldSubscribe()
        //{
        //    var token = new CancellationTokenSource();
        //    var sub = _harness.CreateSubscription("res-tests", "test-context", "*");
        //    var task = sub.Start(_ => { }, DateTime.UtcNow, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0.5), token.Token);
        //    token.Cancel();
        //    Task.WhenAll(task);
        //}

        //[Test]
        //public void ShouldGetSubscribedEvents()
        //{
        //    var m = new AutoResetEvent(false);
        //    var received = new List<EventInStorage>();
        //    var token = new CancellationTokenSource();
        //    var sub = _harness.CreateSubscription("res-tests", "test-context", "*");
        //    var client = _harness.CreateClient();
        //    client.CommitAsync("test-context", "test-stream", new[]
        //    {
        //        new EventData("test", Guid.NewGuid(), "", "a bit more", DateTime.Now)
        //    }, ExpectedVersion.Any).Wait(1000);

        //    var task = sub.Start(x =>
        //    {
        //        received.AddRange(x.Events);
        //        m.Set();
        //    }, DateTime.Now.AddSeconds(-10), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0.5), token.Token);


        //    m.WaitOne();

        //    token.Cancel();

        //    Task.WhenAny(task);

        //    Assert.AreEqual(1, received.Count);
        //}

        //[Test]
        //public void ShouldSetSubscriptionTime()
        //{
        //    var received = new BlockingCollection<EventInStorage>();
        //    var token = new CancellationTokenSource();
        //    var sub = _harness.CreateSubscription("res-tests", "test-context", "*");
        //    var client = _harness.CreateClient();

        //    var now = DateTime.Now;

        //    client.CommitAsync("test-context", "test-stream", new[]
        //    {
        //        new EventData("test", Guid.NewGuid(), "", "a bit more", now)
        //    }, ExpectedVersion.Any).Wait(1000);

        //    var subscribeTask = sub.Start(x =>
        //    {
        //        received.Add(x.Events[0]);
        //    }, DateTime.Now.AddSeconds(10), TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(0.5), token.Token);

        //    //no events as subscription starts in the future.
        //    subscribeTask.Wait(2000);

        //    sub.SetSubscriptionTime(now.AddSeconds(-5), TimeSpan.FromSeconds(5)).Wait(token.Token);

        //    EventInStorage e;
        //    Assert.IsTrue(received.TryTake(out e, 5000));
        //}


        //[Test]
        ////Kept to ensure nasty race condition doesn't come up.
        //public void ShouldSetSubscriptionTime2()
        //{
        //    var received = new BlockingCollection<EventInStorage>();
        //    var token = new CancellationTokenSource();
        //    var sub = _harness.CreateSubscription("res-tests", "test-context", "*");
        //    var client = _harness.CreateClient();

        //    var now = DateTime.Now;

        //    client.CommitAsync("test-context", "test-stream", new[]
        //    {
        //        new EventData("test", Guid.NewGuid(), "", "a bit more", now)
        //    }, ExpectedVersion.Any).Wait(1000);

        //    var subscribeTask = sub.Start(x =>
        //    {
        //        received.Add(x.Events[0]);
        //    }, DateTime.Now.AddSeconds(10), TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(0.5), token.Token);

        //    //no events as subscription starts in the future.
        //    subscribeTask.Wait(2000);

        //    sub.SetSubscriptionTime(now.AddSeconds(-5), TimeSpan.FromSeconds(5)).Wait(token.Token);

        //    EventInStorage e;
        //    Assert.IsTrue(received.TryTake(out e, 5000));
        //}

        

        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["ResIntegrationTest"].ConnectionString;

        void ClearResStore()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();

                using (var cmd = new SqlCommand("truncate table Subscriptions;", sqlConnection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("truncate table EventWrappers;", sqlConnection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("truncate table Streams;", sqlConnection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("truncate table QueueAllocations;", sqlConnection))
                {
                    cmd.ExecuteNonQuery();
                }
                
                using (var cmd = new SqlCommand("truncate table Queues;", sqlConnection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    public class ResHarness
    {
        public static string Endpoint = ConfigurationManager.AppSettings["resPublishEndpoint"];
        public static string QueryEndpoint = ConfigurationManager.AppSettings["resQueryEndpoint"];
        public static string ResExePath = ConfigurationManager.AppSettings["resExePath"];
        private Process _process;
        private ResPublishEngine _publishEngine;
        private ResQueryEngine _queryEngine;

        public void Start()
        {          
            var start = new ProcessStartInfo(ResExePath, "-endpoint:" + Endpoint);

            _process = Process.Start(start);
            
            _publishEngine = new ResPublishEngine();
            _publishEngine.Start(Endpoint);

            _queryEngine = new ResQueryEngine(QueryEndpoint);
        }

        public ResPublisher CreatePublisher()
        {
            return _publishEngine.CreatePublisher(TimeSpan.FromSeconds(10));      
        }

        public ResQueryClient CreateQueryClient()
        {
            return _queryEngine.CreateClient(TimeSpan.FromSeconds(10));
        }

        public void Stop()
        {
            Console.WriteLine("Disposing.");
            _queryEngine.Dispose();
            _publishEngine.Dispose();
            _process.Kill();
        }
    }
}