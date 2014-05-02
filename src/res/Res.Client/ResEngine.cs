using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using NetMQ;
using Res.Client.Internal;

namespace Res.Client
{
    public class ResEngine : IDisposable
    {
        private readonly bool _isDisabled;
        private CommitRequestAcceptor _acceptor;

        private readonly ILog _log = LogManager.GetCurrentClassLogger();

        private RequestProcessor _processor;
        public ResEngine(bool isDisabled = false)
        {
            _isDisabled = isDisabled;
        }

        public void Start(string endpoint)
        {
            if (_isDisabled)
            {
                _log.Warn("[ResEngine] ResEngine is disabled. Requested clients will be dummies, and will immediately return default values instead of committing events.");
                return;
            }

            _log.Info("[ResEngine] Starting...");

            const int bufferSize = 11;

            TimeSpan reaperForDeadTasksInterval = TimeSpan.FromSeconds(2);

            var buffer = new MultiWriterSingleReaderBuffer(bufferSize);
            _acceptor = new CommitRequestAcceptor(buffer);

            //important: socket needs to be created on request processor main thread. 
            Func<ResGateway> gatewayFactory = () => new SingleThreadedZeroMqGateway(endpoint, reaperForDeadTasksInterval);
            _processor = new RequestProcessor(gatewayFactory, buffer);
            _processor.Start();
            
            _log.Info("[ResEngine] Started.");
        }

        public ResClient CreateClient(TimeSpan defaultTimeout)
        {
            if(_isDisabled)
                return new DummyResClient();

            return new ThreadsafeResClient(_acceptor, defaultTimeout);
        }

        public ResClientEventPublisher CreatePublisher(string context, TimeSpan defaultTimeout, TypeTagResolver typeTagResolver, Func<object, string> serialiser)
        {
            return CreatePublisher(context, CreateClient(defaultTimeout), typeTagResolver, serialiser);
        }

        public ResClientEventPublisher CreatePublisher(string context, ResClient client, TypeTagResolver typeTagResolver, Func<object, string> serialiser)
        {
            return new ResClientEventPublisher(context, client, typeTagResolver, serialiser);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (_isDisabled)
            {
                _log.Info("[ResEngine] Created as disabled. Disposing.");
                return;
            }

            _log.Info("[ResEngine] Stopping...");
            _processor.Stop();
            _log.Info("[ResEngine] Processor stopped. Bye bye.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}