using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Res.Client;
using Res.Client.Exceptions;
using Res.Core.Storage;

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
            ClearResStore();
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
            }
        }
    }

    public class ResHarness
    {
        public const string Endpoint = "tcp://127.0.0.1:9099";
        private Process _process;
        private ResEngine _engine;

        public void Start()
        {
            
#if DEBUG
            var start = new ProcessStartInfo(@"..\..\..\Res\bin\debug\res.exe", "-endpoint:" + Endpoint);
#else
            var start = new ProcessStartInfo(@"..\..\..\Res\bin\release\res.exe", "-endpoint:" + Endpoint);
#endif
            _process = Process.Start(start);
            
            _engine = new ResEngine();
            _engine.Start(Endpoint);
        }

        public ResClient CreateClient()
        {
            return _engine.CreateClient(TimeSpan.FromSeconds(10));      
        }

        public void Stop()
        {
            _engine.Dispose();
            _process.Kill();
        }
    }
}