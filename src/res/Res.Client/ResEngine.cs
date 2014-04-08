using System;
using System.Threading;
using System.Threading.Tasks;
using Res.Client.Internal;

namespace Res.Client
{
    public static class ResEngine
    {
        private static RequestAcceptor _acceptor;

        public static Task Start(string endpoint, CancellationToken token)
        {
            const int bufferSize = 11;
            TimeSpan reaperForDeadTasksInterval = TimeSpan.FromSeconds(30);

            var buffer = new MultiWriterSingleReaderBuffer(bufferSize);
            _acceptor = new RequestAcceptor(buffer);

            var gateway = new SingleThreadedZeroMqGateway(endpoint, reaperForDeadTasksInterval);
            Func<ResGateway> gatewayFactory = () => gateway;
            var processor = new RequestProcessor(gatewayFactory, buffer);
            return processor.Start(token);
        }

        internal static Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion)
        {
            return _acceptor.CommitAsync(context, stream, events, expectedVersion);
        }
    }
}