using System;
using System.Security.Cryptography.X509Certificates;
using Res.Client.Internal;
using Res.Client.Internal.Logging;
using Res.Client.Internal.Queues;

namespace Res.Client
{
    public class ResQueueEngine : IDisposable
    {
        private readonly ILog _log = LogProvider.GetCurrentClassLogger();
        private readonly RequestProcessor _processor;
        private readonly QueueRequestAcceptor _acceptor;

        public ResQueueEngine(string endpoint)
        {
            _log.DebugFormat("[ResQueueEngine] Starting at {0}...", endpoint);

            const int bufferSize = 11;

            TimeSpan reaperForDeadTasksInterval = TimeSpan.FromSeconds(2);

            var buffer = new MultiWriterSingleReaderBuffer(bufferSize);
            _acceptor = new QueueRequestAcceptor(buffer);
            
            //important: socket needs to be created on request processor main thread. 
            Func<ResGateway> gatewayFactory = () => new SingleThreadedZeroMqGateway(endpoint, reaperForDeadTasksInterval);
            _processor = new RequestProcessor(gatewayFactory, buffer);
            _processor.Start();

            _log.Debug("[ResQueueEngine] Started.");
        }

        public ResQueue Declare(string context, string queueId, string subscriberId, string filter, DateTime startTime)
        {
            return new ResQueue(context, queueId, subscriberId, filter, startTime, _acceptor);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _log.Debug("[ResQueueEngine] Stopping...");
            _processor.Stop();
            _log.Debug("[ResQueueEngine] Processor stopped. Bye bye.");
        }
    }
}