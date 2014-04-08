using System;
using System.Threading;
using System.Threading.Tasks;

namespace Res.Client
{
    public class RequestProcessor
    {
        private readonly Func<ResGateway> _gatewayFactory;
        private readonly MultiWriterSingleReaderBuffer _buffer;

        public RequestProcessor(Func<ResGateway> gatewayFactory, MultiWriterSingleReaderBuffer buffer)
        {
            _gatewayFactory = gatewayFactory;
            _buffer = buffer;
        }

        public Task Start(CancellationToken token)
        {
            return Task.Factory.StartNew(() => run(token), token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private void run(CancellationToken token)
        {
            var gateway = _gatewayFactory();

            var spin = new SpinWait();

            while (token.IsCancellationRequested == false)
            {
                bool processed = gateway.ProcessResponse();

                PendingResRequest pendingResRequest;
             
                if (_buffer.TryDequeue(out pendingResRequest))
                {
                    if (pendingResRequest.ShouldDrop())
                    {
                        pendingResRequest.Drop();
                    }
                    else
                    {
                        gateway.SendRequest(pendingResRequest);
                        processed = true;
                    }
                }

                if(!processed)
                    spin.SpinOnce();
            }

            gateway.Dispose();
        }
    }
}