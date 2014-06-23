using System;
using System.Globalization;
using System.Threading;
using Common.Logging;
using NetMQ;
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
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
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
            var queueId = message.Pop().ConvertToString();
            var subscriberId = message.Pop().ConvertToString();
            var context = message.Pop().ConvertToString();
            var filter = message.Pop().ConvertToString();
            var utcStartTime = DateTime.FromBinary(long.Parse(message.Pop().ConvertToString()));
            var allocationSize = int.Parse(message.Pop().ConvertToString());
            var allocationTimeInMilliseconds = int.Parse(message.Pop().ConvertToString());

            var subscribe = new SubscribeToQueue(
                queueId,
                subscriberId,
                context,
                filter,
                utcStartTime,
                allocationSize,
                allocationTimeInMilliseconds
                );

            var queuedEvents = _storage.Subscribe(subscribe);
            var events = queuedEvents.Events;

            var msg = new NetMQMessage();
            msg.Append(sender);
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(requestId);
            msg.Append(ResCommands.QueuedEvents);
            msg.Append(queueId);
            msg.Append(subscriberId);
            msg.Append(DateTime.UtcNow.ToBinary().ToString(CultureInfo.InvariantCulture));

            if (queuedEvents.AllocationId.HasValue)
                msg.Append(queuedEvents.AllocationId.Value.ToString(CultureInfo.InvariantCulture));
            else
                msg.AppendEmptyFrame();

            var count = events.Length;
            msg.Append(count.ToString(CultureInfo.InvariantCulture));

            foreach (var e in events)
            {
                msg.Append(e.EventId.ToByteArray());
                msg.Append(e.Stream);
                msg.Append(e.Context);
                msg.Append(e.Sequence.ToString(CultureInfo.InvariantCulture));
                msg.Append(e.Timestamp.ToBinary().ToString(CultureInfo.InvariantCulture));
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