using System;
using Res.Client.Internal;
using Res.Client.Internal.Commits;
using Res.Client.Internal.Logging;

namespace Res.Client
{
    public class ResPublishEngine : IDisposable
    {
        private readonly bool _isDisabled;
        private CommitRequestAcceptor _acceptor;

        private readonly ILog _log = LogProvider.GetCurrentClassLogger();

        private RequestProcessor _processor;
        public ResPublishEngine(string endpoint, bool isDisabled = false)
        {
            _isDisabled = isDisabled;
            start(endpoint);
        }

        private void start(string endpoint)
        {
            if (_isDisabled)
            {
                _log.Warn("[ResPublishEngine] ResPublishEngine is disabled. Requested clients will be dummies, and will immediately return default values instead of committing events.");
                return;
            }

            _log.Debug("[ResPublishEngine] Starting...");

            const int bufferSize = 11;

            TimeSpan reaperForDeadTasksInterval = TimeSpan.FromSeconds(2);

            var buffer = new MultiWriterSingleReaderBuffer(bufferSize);
            _acceptor = new CommitRequestAcceptor(buffer);

            //important: socket needs to be created on request processor main thread. 
            Func<ResGateway> gatewayFactory = () => new SingleThreadedZeroMqGateway(endpoint, reaperForDeadTasksInterval);
            _processor = new RequestProcessor(gatewayFactory, buffer);
            _processor.Start();
            
            _log.Debug("[ResPublishEngine] Started.");
        }

        public ResPublisher CreateRawPublisher(TimeSpan defaultTimeout)
        {
            if(_isDisabled)
                return new DummyResPublisher();

            return new ThreadsafeResPublisher(_acceptor, defaultTimeout);
        }

        public ResClientEventPublisher CreateEventPublisher(string context, TimeSpan defaultTimeout, TypeTagResolver typeTagResolver, Func<object, string> serialiser)
        {
            return CreateEventPublisher(context, CreateRawPublisher(defaultTimeout), typeTagResolver, serialiser);
        }

        public ResClientEventPublisher CreateEventPublisher(string context, ResPublisher publisher, TypeTagResolver typeTagResolver, Func<object, string> serialiser)
        {
            return new ResClientEventPublisher(context, publisher, typeTagResolver, serialiser);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (_isDisabled)
            {
                _log.Debug("[ResPublishEngine] Created as disabled. Disposing.");
                return;
            }

            _log.Debug("[ResPublishEngine] Stopping...");
            _processor.Stop();
            _log.Debug("[ResPublishEngine] Processor stopped. Bye bye.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}