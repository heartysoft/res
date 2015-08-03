using System;
using System.Globalization;
using NetMQ;
using Res.Client.Exceptions;
using Res.Protocol;

namespace Res.Client.Internal.Queues.Messages
{
    public class SubscribeToQueueRequest : ResRequest
    {
        private readonly string _queueId;
        private readonly string _subscriberId;
        private readonly string _context;
        private readonly string _filter;
        private readonly DateTime _utcStartTime;
        private readonly int _allocationBatchSize;
        private readonly int _allocationTimeInMilliseconds;

        public SubscribeToQueueRequest(string context, string queueId, string subscriberId, string filter, DateTime utcStartTime, int allocationBatchSize, int allocationTimeInMilliseconds)
        {
            _queueId = queueId;
            _subscriberId = subscriberId;
            _context = context;
            _filter = filter;
            _utcStartTime = utcStartTime;
            _allocationBatchSize = allocationBatchSize;
            _allocationTimeInMilliseconds = allocationTimeInMilliseconds;
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, string requestId)
        {
            var pending = (PendingResRequest<QueuedEventsResponse>) pendingRequest;

            var msg = new NetMQMessage();
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(ResCommands.SubscribeToQueue);
            msg.Append(requestId);

            msg.Append(_context);
            msg.Append(_queueId);
            msg.Append(_subscriberId);
            msg.Append(_filter);
            msg.Append(_utcStartTime.ToBinary().ToString(CultureInfo.InvariantCulture));
            msg.Append(_allocationBatchSize.ToString(CultureInfo.InvariantCulture));
            msg.Append(_allocationTimeInMilliseconds.ToString(CultureInfo.InvariantCulture));

            socket.SendMessage(msg);

            return m =>
            {
                var command = m.Pop().ConvertToString();

                if (command == ResCommands.Error)
                {
                    var errorCode = m.Pop().ConvertToString();
                    var errorDetails = m.Pop().ConvertToString();
                    ErrorResolver.RaiseException(errorCode, errorDetails, pending.SetException);
                    return;
                }

                if (command != ResCommands.QueuedEvents)
                    pending.SetException(new UnsupportedCommandException(command));

                var queuecontext = m.Pop().ConvertToString();
                var queueId = m.Pop().ConvertToString();
                var subscriberId = m.Pop().ConvertToString();
                var time = DateTime.FromBinary(long.Parse(m.Pop().ConvertToString()));

                var allocationFrame = m.Pop();

                long? allocationId = null;

                if (allocationFrame.BufferSize != 0)
                    allocationId = long.Parse(allocationFrame.ConvertToString());

                var count = int.Parse(m.Pop().ConvertToString());

                var events = new EventInStorage[count];

                for (var i = 0; i < count; i++)
                {
                    var id = new Guid(m.Pop().ToByteArray());
                    var streamId = m.Pop().ConvertToString();
                    var context = m.Pop().ConvertToString();
                    var sequence = long.Parse(m.Pop().ConvertToString());
                    var timestamp = DateTime.FromBinary(long.Parse(m.Pop().ConvertToString()));
                    var type = m.Pop().ConvertToString();
                    var headers = m.Pop().ConvertToString();
                    var body = m.Pop().ConvertToString();

                    events[i] = new EventInStorage(context, streamId, sequence, type, id, headers, body, timestamp);
                }

                var result = new QueuedEventsResponse(queuecontext, queueId, subscriberId, time, allocationId, events);
                pending.SetResult(result);
            };
        }
    }
}