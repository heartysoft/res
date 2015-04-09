using System;
using System.Threading.Tasks;
using Res.Client.Internal;
using Res.Client.Internal.Logging;
using Res.Client.Internal.Queries;

namespace Res.Client
{
    public class ResQueryEngine : IDisposable
    {
        private readonly ILog _log = LogProvider.GetCurrentClassLogger();
        private readonly RequestProcessor _processor;
        private readonly QueryRequestAcceptor _acceptor;

        public ResQueryEngine(string endpoint)
        {
            _log.DebugFormat("[ResQueryEngine] Starting at {0}...", endpoint);

            const int bufferSize = 11;

            TimeSpan reaperForDeadTasksInterval = TimeSpan.FromSeconds(2);

            var buffer = new MultiWriterSingleReaderBuffer(bufferSize);
            _acceptor = new QueryRequestAcceptor(buffer);

            //important: socket needs to be created on request processor main thread. 
            Func<ResGateway> gatewayFactory = () => new SingleThreadedZeroMqGateway(endpoint, reaperForDeadTasksInterval);
            _processor = new RequestProcessor(gatewayFactory, buffer);
            _processor.Start();

            _log.Debug("[ResQueryEngine] Started.");
        }

        public ResQueryClient CreateClient(TimeSpan defaultTimeout)
        {
            return new ThreadsafeResQueryClient(_acceptor, defaultTimeout);    
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _log.Debug("[ResQueryEngine] Stopping...");
            _processor.Stop();
            _log.Debug("[ResQueryEngine] Processor stopped. Bye bye.");
        }
    }

    public interface ResQueryClient
    {
        Task<EventsForStream> LoadEvents(string context, string stream, long fromVersion, long? maxVersion, TimeSpan? timeout);
    }

    public class ThreadsafeResQueryClient : ResQueryClient
    {
        private readonly QueryRequestAcceptor _acceptor;
        private readonly TimeSpan _defaultTimeout;

        public ThreadsafeResQueryClient(QueryRequestAcceptor acceptor, TimeSpan defaultTimeout)
        {
            _acceptor = acceptor;
            _defaultTimeout = defaultTimeout;
        }

        public async Task<EventsForStream> LoadEvents(string context, string stream, long fromVersion, long? maxVersion, TimeSpan? timeout)
        {
            var events = await _acceptor.QueryByStream(context, stream, fromVersion, maxVersion, timeout ?? _defaultTimeout);
            return new EventsForStream(events.Context, events.Stream, events.Events);
        }
    }
}