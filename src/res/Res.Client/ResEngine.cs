using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using NetMQ;
using Res.Client.Internal;

namespace Res.Client
{
    public class ResEngine : IDisposable
    {
        private CommitRequestAcceptor _acceptor;

        private readonly ILog _log = LogManager.GetCurrentClassLogger();

        private RequestProcessor _processor;

        public void Start(string endpoint)
        {
            _log.Info("[ResEngine] Starting...");

            const int bufferSize = 11;

            TimeSpan reaperForDeadTasksInterval = TimeSpan.FromSeconds(2);

            var buffer = new MultiWriterSingleReaderBuffer(bufferSize);
            _acceptor = new CommitRequestAcceptor(buffer);

            //important: socket needs to be created on request processor main thread. 
            Func<ResGateway> gatewayFactory = () => new SingleThreadedZeroMqGateway(endpoint, reaperForDeadTasksInterval);
            _processor = new RequestProcessor(gatewayFactory, buffer);
            _processor.Start();
            
            _log.Info("[ResEngine] Started.");
        }

        public ResClient CreateClient(TimeSpan defaultTimeout)
        {
            return new ThreadsafeResClient(_acceptor, defaultTimeout);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _log.Info("[ResEngine] Stopping...");
            _processor.Stop();
            _log.Info("[ResEngine] Processor stopped. Bye bye.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}