using System;
using System.Threading;
using System.Threading.Tasks;
using Res.Client.Internal.Logging;
using Task = System.Threading.Tasks.Task;

namespace Res.Client.Internal
{
    public class RequestProcessor
    {
        private readonly Func<ResGateway> _gatewayFactory;
        private readonly MultiWriterSingleReaderBuffer _buffer;
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        public RequestProcessor(Func<ResGateway> gatewayFactory, MultiWriterSingleReaderBuffer buffer)
        {
            _gatewayFactory = gatewayFactory;
            _buffer = buffer;
        }

        readonly object _startLock = new object();
        private bool _started;
        private CancellationTokenSource _token;
        private Task _task;

        public void Start()
        {
            lock (_startLock)
            {
                if (!_started)
                {
                    _token = new CancellationTokenSource();
                    Log.Debug("[RequestProcessor] Starting up.");
                    _started = true;
                    _task = Task.Factory.StartNew(() => run(_token.Token), _token.Token, TaskCreationOptions.LongRunning,
                        TaskScheduler.Default);
                }
            }
        }

        private void run(CancellationToken token)
        {
            using (var gateway = _gatewayFactory())
            {
                var spin = new SpinWait();

                try
                {
                    Log.DebugFormat("[RequestProcessor] Entering mainloop. Thread Id: {0}",
                        Thread.CurrentThread.ManagedThreadId);

                    while (token.IsCancellationRequested == false)
                    {
                        bool processed = gateway.ProcessResponse();

                        PendingResRequest pendingResRequest;

                        while (_buffer.TryDequeue(out pendingResRequest))
                        {
                            if (pendingResRequest.ShouldDrop())
                            {
                                pendingResRequest.Drop();
                                continue;
                            }

                            gateway.SendRequest(pendingResRequest);
                            processed = true;
                            break;
                        }

                        if (!processed)
                            spin.SpinOnce();
                    }

                    Log.DebugFormat("[RequestProcessor] Exiting mainloop. Thread ID: {0}",
                        Thread.CurrentThread.ManagedThreadId);
                }
                catch (Exception e)
                {
                    Log.DebugFormat("[RequestProcessor] Error in mainloop. Thread ID: {0}", e,
                        Thread.CurrentThread.ManagedThreadId);
                }
                finally
                {
                    Log.DebugFormat("[RequestProcessor] Shutting down gateway. Thread ID: {0}",
                        Thread.CurrentThread.ManagedThreadId);
                    gateway.Dispose();
                    Log.DebugFormat("[RequestProcessor] Gateway shutdown. Thread ID: {0}",
                        Thread.CurrentThread.ManagedThreadId);
                }
            }
        }

        public void Stop()
        {
            _token.Cancel();
            _task.Wait();
        }
    }
}