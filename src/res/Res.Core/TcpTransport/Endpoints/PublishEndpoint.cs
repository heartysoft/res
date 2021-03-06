﻿using System;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NLog;
using Res.Core.StorageBuffering;
using Res.Core.TcpTransport.Commits;
using Res.Core.TcpTransport.MessageProcessing;
using Res.Core.TcpTransport.NetworkIO;
using Res.Protocol;

namespace Res.Core.TcpTransport.Endpoints
{
    public class PublishEndpoint : IDisposable
    {
        private readonly NetMQContext _ctx;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Transceiver _transceiver;
        private Task _transceiverTask;

        public PublishEndpoint(EventStorageWriter eventStorageWriter, ResConfiguration config) 
        {
            var ctx = NetMQContext.Create();
            _ctx = ctx;

            var outBuffer = new OutBuffer(config.PublishEndpoint.BufferSize);
            var dispatcher = new TcpMessageDispatcher();

            dispatcher.Register(ResCommands.AppendCommit, new CommitHandler(eventStorageWriter, outBuffer));
            
            MessageProcessor messageProcessor = new TcpIncomingMessageProcessor(dispatcher);
            messageProcessor = new ErrorHandlingMessageProcessor(messageProcessor);

            //important...the factory method parameter must "create" the gateway, threading issue otherwise.
            Logger.Debug("[CommitEndpoint] Initialising Transceiver. Endpoint: {0}", config.PublishEndpoint.Endpoint);
            _transceiver = new Transceiver(() => new TcpGateway(ctx, config.PublishEndpoint.Endpoint, messageProcessor), outBuffer);
        }

        public void Start(CancellationToken token)
        {
            Logger.Info("[CommitEndpoint] Starting. Shall we begin?");
            _transceiverTask = _transceiver.Start(token);
            Logger.Info("[CommitEndpoint] Started. Reporting for duty...");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            Logger.Info("[CommitEndpoint] Attempting shutdown....");
            _ctx.Dispose();
            _transceiverTask.Wait();
            Logger.Info("[CommitEndpoint] Context disposed. Goodbye, world...");
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}