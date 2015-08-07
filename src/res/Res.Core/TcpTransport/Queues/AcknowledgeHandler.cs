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
    public class AcknowledgeHandler : RequestHandler
    {
        private readonly QueueStorage _storage;
        private readonly OutBuffer _outBuffer;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private SpinWait _spin;

        public AcknowledgeHandler(QueueStorage storage, OutBuffer outBuffer)
        {
            _storage = storage;
            _outBuffer = outBuffer;
            _spin = new SpinWait();
        }

        public void Handle(NetMQFrame[] sender, NetMQMessage message)
        {
            Logger.Debug("[Queue_AcknowledgeHandler] Received an ack.");

            var requestId = message.Pop();
            var context = message.Pop().ConvertToString();
            var queueId = message.Pop().ConvertToString();
            var subscriberId = message.Pop().ConvertToString();

            var allocationId = message.PopNullableInt64();
            var allocationSize = message.PopInt32();
            var allocationTimeInMilliseconds = message.PopInt32();

            var ack = new AcknowledgeQueue(context,
                queueId,
                subscriberId,
                allocationId,
                allocationSize, allocationTimeInMilliseconds);

            var queuedEvents = _storage.AcknowledgeAndFetchNext(ack);
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
                msg.Append(e.TypeKey ?? string.Empty);
                msg.Append(e.Headers ?? string.Empty);
                msg.Append(e.Body ?? string.Empty);
            }

            var result = new QueuedMessagesFetched(msg);
            while (!_outBuffer.Offer(result))
                _spin.SpinOnce();
        }
    }
}