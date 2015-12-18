using System;
using System.Configuration;
using System.Threading;
using NLog;
using Res.Core.Storage;
using Res.Core.Storage.InMemoryQueueStorage;
using Res.Core.StorageBuffering;
using Res.Core.TcpTransport;
using Res.Core.TcpTransport.Endpoints;

namespace Res.Core
{
    public class ResServer : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private CancellationTokenSource _cancellationToken;
        private PublishEndpoint _publishEndpoint;
        private QueueEndpoint _queueEndpoint;
        private QueryEndpoint _queryEndpoint;

        public void Start(ResConfiguration config)
        {
            Logger.Info("[ResHost] Starting...Geronimo....");
            _cancellationToken = new CancellationTokenSource();
            var eventStorage = getEventStorage(config.ConnectionStringName);
            var storageWriter = new EventStorageWriter(config.Writer.BufferSize, config.Writer.TimeoutBeforeDrop,
                eventStorage, config.Writer.BatchSize);
            storageWriter.Start(_cancellationToken.Token);
            var storageReader = new EventStorageReader(config.Reader.BufferSize, config.Reader.TimeoutBeforeDrop,
                eventStorage, config.Reader.BatchSize);

            _publishEndpoint = new PublishEndpoint(storageWriter, config);
            _publishEndpoint.Start(_cancellationToken.Token);

            _queryEndpoint = new QueryEndpoint(storageReader, config);
            _queryEndpoint.Start(_cancellationToken.Token);

            var queueStorage = getQueueStorage(config.ConnectionStringName, eventStorage);
            _queueEndpoint = new QueueEndpoint(queueStorage, config);
            _queueEndpoint.Start(_cancellationToken.Token);


            Logger.Info("[ResHost] Started. All systems go...");
        }

        private EventStorage getEventStorage(string connectionStringName)
        {
            if (connectionStringName == "inmem")
            {
                return new InMemoryEventStorage();
            }
            else
            {
                var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
                var eventStorage = new SqlEventStorage(connectionString);
                return eventStorage;
            }
        }

        private QueueStorage getQueueStorage(string connectionStringName, EventStorage eventStorage)
        {
            if (connectionStringName == "inmem")
            {
                if (eventStorage is InMemoryEventStorage == false)
                    throw new ArgumentException(
                        "In memory queue storage can't be used without in memory event storage.", "eventStorage");

                return new InMemoryQueueStorage((InMemoryEventStorage) eventStorage);
            }
            else
            {
                var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
                var queueStorage = new SqlQueueStorage(connectionString);
                return queueStorage;
            }
        }

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Dispose(true);
            }

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                stop();
            }
        }

        private void stop()
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