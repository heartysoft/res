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
        private static CancellationTokenSource _token;

        private static readonly object RunningLock = new object();
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private static bool _running;
        private static NetMQContext _ctx;

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
            TimeSpan reaperForDeadTasksInterval = TimeSpan.FromSeconds(5);

            var buffer = new MultiWriterSingleReaderBuffer(bufferSize);
            _acceptor = new RequestAcceptor(buffer);

            _ctx = NetMQContext.Create();
            //important: socket needs to be created on request processor main thread. 
            Func<ResGateway> gatewayFactory = () => new SingleThreadedZeroMqGateway(_ctx, endpoint, reaperForDeadTasksInterval);
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

                _running = false;

                Log.Info("[ResEngine] Stopping...");

                _token.Cancel(true);

                Log.Info("[ResEngine] Socket closed. Disposing context.");
                try
                {
                    _ctx.Dispose();
                    Log.Info("[ResEngine] Context disposed. Bye.");
                }
                catch (Exception e)
                {
                    Log.Info("[ResEngine] Error disposing context.", e);
                }
            }
        }

        internal static Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion, TimeSpan timeout)
        {
            return _acceptor.CommitAsync(context, stream, events, expectedVersion, timeout);
        }

        internal static Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion)
        {
            return CommitAsync(context, stream, events, expectedVersion, TimeSpan.FromSeconds(10));
        }
    }
}