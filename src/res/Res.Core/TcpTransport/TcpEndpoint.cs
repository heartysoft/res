using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using NetMQ;
using Res.Core.StorageBuffering;

namespace Res.Core.TcpTransport
{
    public class TcpEndpoint : IDisposable
    {
        private readonly NetMQContext _ctx;
        private readonly Sink _sink;
        private readonly Receiver _receiver;
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        public TcpEndpoint(string endpoint, EventStorageWriter writer)
        {
            _ctx = NetMQContext.Create();

            _sink = new Sink(_ctx, endpoint);
            var commitAppender = new CommitAppender(writer, _sink);
            var resultProcessor = new ResultProcessor();
            var messageProcessor = new CoreMessageProcessor(commitAppender, resultProcessor);
            _receiver = new Receiver(_ctx, endpoint, messageProcessor);
        }

        public Task Start(CancellationToken token)
        {
            Logger.Info("[TcpEndpoint] Starting. Shall we begin?");
            var receiver = _receiver.Start(token);
            var sink = _sink.Start(token);
            Logger.Info("[TcpEndpoint] Started. Reporting for duty...");
            return Task.WhenAll(receiver, sink);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            Logger.Info("[TcpEndpoint] Attempting shutdown....");
            _ctx.Dispose();
            Logger.Info("[TcpEndpoint] Context disposed. Goodbye, world...");
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}