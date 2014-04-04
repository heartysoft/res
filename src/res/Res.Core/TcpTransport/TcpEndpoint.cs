using System;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using Res.Core.StorageBuffering;

namespace Res.Core.TcpTransport
{
    public class TcpEndpoint : IDisposable
    {
        private readonly NetMQContext _ctx;
        private readonly Sink _sink;
        private readonly Receiver _receiver;

        public TcpEndpoint(string endpoint, EventStorageWriter writer)
        {
            _ctx = NetMQContext.Create();

            _sink = new Sink(_ctx, endpoint);
            var commitAppender = new CommitAppender(writer, _sink);
            var resultProcessor = new ResultProcessor();
            var messageProcessor = new MessageProcessor(commitAppender, resultProcessor);
            _receiver = new Receiver(_ctx, endpoint, messageProcessor);
        }

        public Task Start(CancellationToken token)
        {
            return Task.WhenAll(_sink.Start(token), _receiver.Start(token));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _ctx.Dispose();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}