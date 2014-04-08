using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Res.Client.Internal;

namespace Res.Client
{
    public static class ResEngine
    {
        private static RequestAcceptor _acceptor;
        private static CancellationTokenSource _token;

        private static readonly object RunningLock = new object();
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private static bool _running;

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

            _token = new CancellationTokenSource();
            TimeSpan reaperForDeadTasksInterval = TimeSpan.FromSeconds(30);

            var buffer = new MultiWriterSingleReaderBuffer(bufferSize);
            _acceptor = new RequestAcceptor(buffer);

            //important: socket needs to be created on request processor main thread. 
            Func < ResGateway > gatewayFactory = () => new SingleThreadedZeroMqGateway(endpoint, reaperForDeadTasksInterval);
            var processor = new RequestProcessor(gatewayFactory, buffer);
            processor.Start(_token.Token);
            
            Log.Info("[ResEngine] Started.");
        }

        public static void Stop()
        {
            lock (RunningLock)
            {
                if (!_running)
                    return;

                Log.Info("[ResEngine] Stopping...");

                _token.Cancel(true);

                _running = false;
            }
        }

        internal static Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion)
        {
            return _acceptor.CommitAsync(context, stream, events, expectedVersion);
        }
    }
}