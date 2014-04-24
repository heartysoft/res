using System;
using Common.Logging;
using Res.Client.Internal;
using Res.Client.Internal.Subscriptions;

namespace Res.Client
{
    public class ResSubscriptionEngine
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

        public Subscription Subscribe(SubscriptionDefinition[] subscriptions, Action<SubscribedEvents> handler)
        {
            return new Subscription(subscriptions, handler, _acceptor);
        }
    }
}