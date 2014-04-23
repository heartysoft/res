﻿using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Res.Core.Storage;
using Res.Core.StorageBuffering;
using Res.Core.TcpTransport;

namespace Res
{
    public class ResHost
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private CancellationTokenSource _cancellationToken;
        private TcpEndpoint _tcpEndpoint;
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
            _tcpEndpoint = new TcpEndpoint(config.TcpEndpoint, storageWriter);
            _tcpEndpoint.Start(_cancellationToken.Token);


            var subscriptionStorage = new SqlSubscriptionStorage(connectionString);
            _queryEndpoint = new QueryEndpoint(subscriptionStorage, config);
            _queryEndpoint.Start(_cancellationToken.Token);

            Logger.Info("[ResHost] Started. All systems go...");
        }

        public void Stop()
        {
            Logger.Info("[ResHost] Stopping. Deploying airbrakes...");
            _cancellationToken.Cancel();
            _tcpEndpoint.Dispose();
            _queryEndpoint.Dispose();
            Logger.Info("[ResHost] Stopped. My work is done; it's in your hands now...");
        }
    }
}