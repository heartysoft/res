﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using NetMQ;
using Res.Core.Storage;
using Res.Core.TcpTransport.MessageProcessing;
using Res.Core.TcpTransport.NetworkIO;
using Res.Core.TcpTransport.Subscriptions;
using Res.Protocol;

namespace Res.Core.TcpTransport
{
    public class QueryEndpoint : IDisposable
    {
        private readonly NetMQContext _ctx;
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly Transceiver _transceiver;

        public QueryEndpoint(SubscriptionStorage subscriptionStorage, ResConfiguration config)
        {
            var ctx = NetMQContext.Create();
            _ctx = ctx;

            var outBuffer = new OutBuffer(config.QueryEndpoint.BufferSize);
            var dispatcher = new TcpMessageDispatcher();

            dispatcher.Register(ResCommands.RegisterSubscriptions, new SubscribeHandler(subscriptionStorage, outBuffer));
            dispatcher.Register(ResCommands.FetchEvents, new FetchEventsHandler(subscriptionStorage, outBuffer));
            dispatcher.Register(ResCommands.ProgressSubscriptions, new ProgressSubscriptionHandler(subscriptionStorage, outBuffer));
            
            MessageProcessor messageProcessor = new TcpIncomingMessageProcessor(dispatcher);
            messageProcessor = new ErrorHandlingMessageProcessor(messageProcessor);

            //important...the factory method parameter must "create" the gateway, threading issue otherwise.
            _transceiver = new Transceiver(() => new TcpGateway(ctx, config.QueryEndpoint.Endpoint, messageProcessor), outBuffer);
        }

        public Task Start(CancellationToken token)
        {
            Logger.Info("[QueryEndpoint] Starting. Shall we begin?");
            var transceiver = _transceiver.Start(token);
            Logger.Info("[QueryEndpoint] Started. Reporting for duty...");
            return transceiver;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            Logger.Info("[QueryEndpoint] Attempting shutdown....");
            _ctx.Dispose();
            Logger.Info("[QueryEndpoint] Context disposed. Goodbye, world...");
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}