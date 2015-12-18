using System;
using System.Configuration;
using System.Diagnostics;
using Res.Client;
using Res.Core.TcpTransport;

namespace Res.Core.Tests.Client
{
    public class ResHarness
    {
        public static string Endpoint = ConfigurationManager.AppSettings["resPublishEndpoint"];
        public static string QueryEndpoint = ConfigurationManager.AppSettings["resQueryEndpoint"];
        public static string QueueEndpoint = ConfigurationManager.AppSettings["resQueueEndpoint"];
        public static string ResExePath = ConfigurationManager.AppSettings["resExePath"];
        private Process _process;
        private ResPublishEngine _publishEngine;
        private ResQueryEngine _queryEngine;
        private ResQueueEngine _queueEngine;
        private ResServer _server;

        public void StartClient()
        {          
            _publishEngine = new ResPublishEngine(Endpoint);

            _queryEngine = new ResQueryEngine(QueryEndpoint);
            _queueEngine = new ResQueueEngine(QueueEndpoint);
        }

        public void StartExternalServer()
        {
            var start = new ProcessStartInfo(ResExePath, "-endpoint:" + Endpoint);
            _process = Process.Start(start);
        }

        public void StartInMemoryServer()
        {
            _server = new ResServer();
            _server.Start(new ResConfiguration()
            {
                ConnectionStringName = "inmem",
                PublishEndpoint = new PublishEndpointConfiguration() { BufferSize = 100, Endpoint = Endpoint},
                QueryEndpoint = new QueryEndpointConfiguration() { BufferSize = 100, Endpoint = QueryEndpoint},
                QueueEndpoint = new QueueEndpointConfiguration() { BufferSize = 100, Endpoint = QueueEndpoint},
                Reader = new StorageBufferConfiguration() { BatchSize = 100, BufferSize = 100, TimeoutBeforeDrop = TimeSpan.FromSeconds(10)},
                Writer = new StorageBufferConfiguration() { BatchSize = 100, BufferSize = 100, TimeoutBeforeDrop = TimeSpan.FromSeconds(10) }
            });
        }


        public ResPublisher CreatePublisher()
        {
            return _publishEngine.CreateRawPublisher(TimeSpan.FromSeconds(10));      
        }

        public ResQueryClient CreateQueryClient()
        {
            return _queryEngine.CreateClient(TimeSpan.FromSeconds(10));
        }

        public void Stop()
        {
            _queueEngine.Dispose();
            _queryEngine.Dispose();
            _publishEngine.Dispose();

            if(_process != null)
                _process.Kill();

            if(_server != null)
                _server.Dispose();
        }

        public ResQueueEngine QueueEngine { get { return _queueEngine; } }
    }
}