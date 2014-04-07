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

            while (token.IsCancellationRequested == false)
            {
                gateway.ProcessResponse();

                MultiWriterSingleReaderBuffer.Entry entry;
                if (_buffer.TryDequeue(out entry))
                {
                    
                }
            }
        }
    }
}