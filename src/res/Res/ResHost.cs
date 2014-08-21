using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Res.Core.Storage;
using Res.Core.StorageBuffering;
using Res.Core.TcpTransport;
using Res.Core.TcpTransport.Commits;
using Res.Core.TcpTransport.Endpoints;

namespace Res
{
    public class ResHost
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private CancellationTokenSource _cancellationToken;
        private PublishEndpoint _publishEndpoint;
        private QueueEndpoint _queueEndpoint;
        private QueryEndpoint _queryEndpoint;

        public void Start(ResConfiguration config)
        {
            Logger.Info("[ResHost] Starting...Geronimo....");
            _cancellationToken = new CancellationTokenSource();
            var connectionString = ConfigurationManager.ConnectionStrings[config.ConnectionStringName].ConnectionString;
            var eventStorage = new SqlEventStorage(connectionString);
            var storageWriter = new EventStorageWriter(config.Writer.BufferSize, config.Writer.TimeoutBeforeDrop,
                eventStorage, config.Writer.BatchSize);
            storageWriter.Start(_cancellationToken.Token);
            var storageReader = new EventStorageReader(config.Reader.BufferSize, config.Reader.TimeoutBeforeDrop,
                eventStorage, config.Reader.BatchSize);

            _publishEndpoint = new PublishEndpoint(storageWriter, config);
            _publishEndpoint.Start(_cancellationToken.Token);

            _queryEndpoint = new QueryEndpoint(storageReader, config);
            _queryEndpoint.Start(_cancellationToken.Token);
            
            var queueStorage = new SqlQueueStorage(connectionString);
            _queueEndpoint = new QueueEndpoint(queueStorage, config);
            _queueEndpoint.Start(_cancellationToken.Token);


            Logger.Info("[ResHost] Started. All systems go...");
        }

        public void Stop()
        {
            Logger.Info("[ResHost] Stopping. Deploying airbrakes...");
            _cancellationToken.Cancel();
            _queueEndpoint.Dispose();
            _queryEndpoint.Dispose();
            _publishEndpoint.Dispose();
            Logger.Info("[ResHost] Stopped. My work is done, it's in your hands now...");
        }
    }
}