using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Res.Client;

namespace Res.Core.Tests.Client
{
    [TestFixture]
    public class TcpClientTests
    {
        private ResHarness _harness;

        [SetUp]
        public void SetupAll()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            _harness = new ResHarness();
            _harness.Start();
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Wawa {0}", e.ExceptionObject);
        }

        [TearDown]
        public void TeardownAll()
        {
            _harness.Stop();
        }


        [Test]
        public void ShouldBootUpAndTeardown()
        {
            var client = new ThreadsafeResClient();
        }

        [Test]
        public void ShouldStoreAnEvent()
        {
            var client = new ThreadsafeResClient();
            var commit = client.CommitAsync("test-context", Guid.NewGuid().ToString(), new[]
            {
                new EventData("test", Guid.NewGuid(), "", "some body", DateTime.Now) 
            }, 0);

            commit.Wait();
        }
    }

    public class ResHarness
    {
        public const string Endpoint = "tcp://127.0.0.1:9099";
        private Process _process;

        public void Start()
        {
            var start = new ProcessStartInfo(@"..\..\..\Res\bin\debug\res.exe", "-endpoint:" + Endpoint);
            _process = Process.Start(start);
            ResEngine.Start(Endpoint);
        }

        public void Stop()
        {
            ResEngine.Stop();
            _process.Kill();
        }
    }
}