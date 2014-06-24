using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using NetMQ;
using Res.Core.Storage;
using Res.Core.StorageBuffering;
using Res.Core.TcpTransport.MessageProcessing;
using Res.Core.TcpTransport.NetworkIO;
using Res.Core.TcpTransport.Queries;
using Res.Protocol;

namespace Res.Core.TcpTransport.Endpoints
{
    public class QueryEndpoint : IDisposable
    {
        private readonly NetMQContext _ctx;
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly Transceiver _transceiver;
        private Task _transceiverTask;

        public QueryEndpoint(EventStorageReader reader, ResConfiguration config)
        {
            var ctx = NetMQContext.Create();
            _ctx = ctx;

            var outBuffer = new OutBuffer(config.QueryEndpoint.BufferSize);
            var dispatcher = new TcpMessageDispatcher();

            dispatcher.Register(ResCommands.QueryEventsByStream, new LoadEventsByStreamHandler(reader, outBuffer));

            MessageProcessor messageProcessor = new TcpIncomingMessageProcessor(dispatcher);
            messageProcessor = new ErrorHandlingMessageProcessor(messageProcessor);

            //important...the factory method parameter must "create" the gateway, threading issue otherwise.
            Logger.DebugFormat("[QueryEndpoint] Initialising Transceiver. Endpoint: {0}", config.QueryEndpoint.Endpoint);
            _transceiver = new Transceiver(() => new TcpGateway(ctx, config.QueryEndpoint.Endpoint, messageProcessor), outBuffer);
        }

        public void Start(CancellationToken token)
        {
            Logger.Info("[QueryEndpoint] Starting. Shall we begin?");
            _transceiverTask = _transceiver.Start(token);
            Logger.Info("[QueryEndpoint] Started. Reporting for duty...");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            Logger.Info("[QueryEndpoint] Attempting shutdown....");
            _ctx.Dispose();
            _transceiverTask.Wait();
            Logger.Info("[QueryEndpoint] Context disposed. Goodbye, world...");
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}