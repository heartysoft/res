using System;
using Common.Logging;
using Res.Client.Internal;
using Res.Client.Internal.Subscriptions;

namespace Res.Client
{
    public class ResSubscriptionEngine : IDisposable
    {
        private SubscriptionRequestAcceptor _acceptor;

        private readonly ILog _log = LogManager.GetCurrentClassLogger();

        private RequestProcessor _processor;

        public void Start(string endpoint)
        {
            _log.Info("[ResSubscriptionEngine] Starting...");

            const int bufferSize = 11;

            TimeSpan reaperForDeadTasksInterval = TimeSpan.FromSeconds(2);

            var buffer = new MultiWriterSingleReaderBuffer(bufferSize);
            _acceptor = new SubscriptionRequestAcceptor(buffer);

            //important: socket needs to be created on request processor main thread. 
            Func<ResGateway> gatewayFactory = () => new SingleThreadedZeroMqGateway(endpoint, reaperForDeadTasksInterval);
            _processor = new RequestProcessor(gatewayFactory, buffer);
            _processor.Start();

            _log.Info("[ResSubscriptionEngine] Started.");
        }

        public Subscription Subscribe(string subscriberId, SubscriptionDefinition[] subscriptions)
        {
            return new Subscription(subscriberId, subscriptions, _acceptor);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _log.Info("[ResSubscriptionEngine] Stopping...");
            _processor.Stop();
            _log.Info("[ResSubscriptionEngine] Processor stopped. Bye bye.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}