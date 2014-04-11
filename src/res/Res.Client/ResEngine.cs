using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using NetMQ;
using Res.Client.Internal;

namespace Res.Client
{
    public static class ResEngine
    {
        private static RequestAcceptor _acceptor;

        private static readonly object RunningLock = new object();
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private static bool _running;
        private static RequestProcessor _processor;

        public static void Start(string endpoint)
        {
            lock (RunningLock)
            {
                if (_running)
                    return;

                _running = true;
            }
            Log.Info("[ResEngine] Starting...");

            const int bufferSize = 11;

            TimeSpan reaperForDeadTasksInterval = TimeSpan.FromSeconds(2);

            var buffer = new MultiWriterSingleReaderBuffer(bufferSize);
            _acceptor = new RequestAcceptor(buffer);

            //important: socket needs to be created on request processor main thread. 
            Func<ResGateway> gatewayFactory = () => new SingleThreadedZeroMqGateway(endpoint, reaperForDeadTasksInterval);
            _processor = new RequestProcessor(gatewayFactory, buffer);
            _processor.Start();
            
            Log.Info("[ResEngine] Started.");
        }

        public static void Stop()
        {
            lock (RunningLock)
            {
                if (!_running)
                    return;

                _running = false;

                Log.Info("[ResEngine] Stopping...");
                _processor.Stop();
                Log.Info("[ResEngine] Processor stopped. Bye bye.");
            }
        }

        internal static Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion, TimeSpan timeout)
        {
            if (!_running)
                return Task.FromResult(default(CommitResponse));

            return _acceptor.CommitAsync(context, stream, events, expectedVersion, timeout);
        }

        internal static Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion)
        {
            return CommitAsync(context, stream, events, expectedVersion, TimeSpan.FromSeconds(10));
        }
    }
}