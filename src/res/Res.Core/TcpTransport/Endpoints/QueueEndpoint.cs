using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using NetMQ;
using Res.Core.Storage;
using Res.Core.TcpTransport.MessageProcessing;
using Res.Core.TcpTransport.NetworkIO;
using Res.Protocol;

namespace Res.Core.TcpTransport.Endpoints
{
    public class QueueEndpoint : IDisposable
    {
        private readonly NetMQContext _ctx;
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly Transceiver _transceiver;
        private Task _transceiverTask;

        public QueueEndpoint(QueueStorage storage, ResConfiguration config)
        {
            var ctx = NetMQContext.Create();
            _ctx = ctx;

            var outBuffer = new OutBuffer(config.QueueEndpoint.BufferSize);
            var dispatcher = new TcpMessageDispatcher();

            dispatcher.Register(ResCommands.SubscribeToQueue, new Queues.SubscribeHandler(storage, outBuffer));
            dispatcher.Register(ResCommands.AcknowledgeQueue, new Queues.AcknowledgeHandler(storage, outBuffer));

            MessageProcessor messageProcessor = new TcpIncomingMessageProcessor(dispatcher);
            messageProcessor = new ErrorHandlingMessageProcessor(messageProcessor);

            //important...the factory method parameter must "create" the gateway, threading issue otherwise.
            Logger.DebugFormat("[QueueEndpoint] Initialising Transceiver. Endpoint: {0}", config.QueueEndpoint.Endpoint);
            _transceiver = new Transceiver(() => new TcpGateway(ctx, config.QueueEndpoint.Endpoint, messageProcessor), outBuffer);
        }

        public void Start(CancellationToken token)
        {
            Logger.Info("[QueueEndpoint] Starting. Shall we begin?");
            _transceiverTask = _transceiver.Start(token);
            Logger.Info("[QueueEndpoint] Started. Reporting for duty...");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            Logger.Info("[QueueEndpoint] Attempting shutdown....");
            _ctx.Dispose();
            _transceiverTask.Wait();
            Logger.Info("[QueueEndpoint] Context disposed. Goodbye, world...");
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}