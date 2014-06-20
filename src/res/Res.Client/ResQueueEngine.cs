using System;
using System.Security.Cryptography.X509Certificates;
using Common.Logging;
using Res.Client.Internal;
using Res.Client.Internal.Queues;

namespace Res.Client
{
    public class ResQueueEngine : IDisposable
    {
        private readonly ILog _log = LogManager.GetCurrentClassLogger();
        private readonly RequestProcessor _processor;
        private readonly QueueRequestAcceptor _acceptor;

        public ResQueueEngine(string endpoint)
        {
            _log.InfoFormat("[ResQueueEngine] Starting at {0}...", endpoint);

            const int bufferSize = 11;

            TimeSpan reaperForDeadTasksInterval = TimeSpan.FromSeconds(2);

            var buffer = new MultiWriterSingleReaderBuffer(bufferSize);
            _acceptor = new QueueRequestAcceptor(buffer);
            
            //important: socket needs to be created on request processor main thread. 
            Func<ResGateway> gatewayFactory = () => new SingleThreadedZeroMqGateway(endpoint, reaperForDeadTasksInterval);
            _processor = new RequestProcessor(gatewayFactory, buffer);
            _processor.Start();

            _log.Info("[ResQueueEngine] Started.");
        }

        public ResQueue Declare(string queueId, string subscriberId, string context, string filter, DateTime startTime)
        {
            return new ResQueue(queueId, subscriberId, context, filter, startTime, _acceptor);
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

            _log.Info("[ResQueueEngine] Stopping...");
            _processor.Stop();
            _log.Info("[ResQueueEngine] Processor stopped. Bye bye.");
        }
    }
}