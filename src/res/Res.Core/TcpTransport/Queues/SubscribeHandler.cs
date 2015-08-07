using System;
using System.Globalization;
using System.Threading;
using NetMQ;
using NLog;
using Res.Core.Storage;
using Res.Core.TcpTransport.MessageProcessing;
using Res.Core.TcpTransport.NetworkIO;
using Res.Protocol;

namespace Res.Core.TcpTransport.Queues
{
    public class SubscribeHandler : RequestHandler
    {
        private readonly QueueStorage _storage;
        private readonly OutBuffer _outBuffer;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private SpinWait _spin;

        public SubscribeHandler(QueueStorage storage, OutBuffer outBuffer)
        {
            _storage = storage;
            _outBuffer = outBuffer;
            _spin = new SpinWait();
        }

        public void Handle(NetMQFrame[] sender, NetMQMessage message)
        {
            Logger.Debug("[Queue_SubscribeHandler] Received subscribe request.");

            var requestId = message.Pop();
            var context = message.Pop().ConvertToString();
            var queueId = message.Pop().ConvertToString();
            var subscriberId = message.Pop().ConvertToString();
            var filter = message.Pop().ConvertToString();
            var utcStartTime = message.PopDateTime();
            var allocationSize = message.PopInt32();
            var allocationTimeInMilliseconds = message.PopInt32();

            var subscribe = new SubscribeToQueue(context,
                queueId,
                subscriberId,
                filter,
                utcStartTime, allocationSize, allocationTimeInMilliseconds);

            var queuedEvents = _storage.Subscribe(subscribe);
            var events = queuedEvents.Events;

            var msg = new NetMQMessage();
            msg.Append(sender);
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(requestId);
            msg.Append(ResCommands.QueuedEvents);
            msg.Append(context);
            msg.Append(queueId);
            msg.Append(subscriberId);
            msg.Append(DateTime.UtcNow.ToNetMqFrame());
            msg.Append(queuedEvents.AllocationId.ToNetMqFrame());
            
            var count = events.Length;
            msg.Append(count.ToNetMqFrame());

            foreach (var e in events)
            {
                msg.Append(e.EventId.ToByteArray());
                msg.Append(e.Stream);
                msg.Append(e.Context);
                msg.Append(e.Sequence.ToNetMqFrame());
                msg.Append(e.Timestamp.ToNetMqFrame());
                msg.Append(e.TypeKey);
                msg.Append(e.Headers.ToNetMqFrame());
                msg.Append(e.Body);
            }

            var result = new QueuedMessagesFetched(msg);
            while (!_outBuffer.Offer(result))
                _spin.SpinOnce();
        }
    }
}