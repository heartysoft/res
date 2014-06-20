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
            var client = _harness.CreateClient();
        }

        [Test]
        public void ShouldStoreAnEvent()
        {
            var client = _harness.CreateClient();
            var commit = client.CommitAsync("test-context", Guid.NewGuid().ToString(), new[]
            {
                new EventData("test", Guid.NewGuid(), "", "some body", DateTime.Now) 
            }, ExpectedVersion.OnlyNew, TimeSpan.FromSeconds(1));

            commit.Wait();
        }

        [Test]
        public void ShouldStoreMultiplEvents()
        {
            var client = _harness.CreateClient();
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
            var client = _harness.CreateClient();
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
            var client = _harness.CreateClient();
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
            var client = _harness.CreateClient();
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
        public static string Endpoint = ConfigurationManager.AppSettings["resEndpoint"];
        public static string SubscriptionEndpoint = ConfigurationManager.AppSettings["resSubscriptionEndpoint"];
        public static string ResExePath = ConfigurationManager.AppSettings["resExePath"];
        private Process _process;
        private ResEngine _engine;
        private ResSubscriptionEngine _subEngine;

        public void Start()
        {          
            var start = new ProcessStartInfo(ResExePath, "-endpoint:" + Endpoint);

            _process = Process.Start(start);
            
            _engine = new ResEngine();
            _engine.Start(Endpoint);

            _subEngine = new ResSubscriptionEngine();
            _subEngine.Start(SubscriptionEndpoint);
        }

        public ResClient CreateClient()
        {
            return _engine.CreateClient(TimeSpan.FromSeconds(10));      
        }


        public Subscription CreateSubscription(string subscriberId, string context, string filter)
        {
            return _subEngine.Subscribe(subscriberId, new[] {new SubscriptionDefinition(context, filter)});
        }
        

        public void Stop()
        {
            Console.WriteLine("Disposing.");
            _engine.Dispose();
            _subEngine.Dispose();
            _process.Kill();
        }
    }
}