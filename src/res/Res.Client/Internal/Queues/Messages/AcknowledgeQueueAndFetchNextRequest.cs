using System;
using System.Globalization;
using NetMQ;
using Res.Client.Exceptions;
using Res.Client.Internal.NetMQ;
using Res.Protocol;

namespace Res.Client.Internal.Queues.Messages
{
    public class AcknowledgeQueueAndFetchNextRequest : ResRequest
    {
        private readonly string _context;
        private readonly string _queueId;
        private readonly string _subscriberId;
        private readonly long? _allocationId;
        private readonly int _allocationBatchSize;
        private readonly int _allocationTimeoutInMilliseconds;

        public AcknowledgeQueueAndFetchNextRequest(string context, string queueId, string subscriberId, long? allocationId, int allocationBatchSize, int allocationTimeoutInMilliseconds)
        {
            _context = context;
            _queueId = queueId;
            _subscriberId = subscriberId;
            _allocationId = allocationId;
            _allocationBatchSize = allocationBatchSize;
            _allocationTimeoutInMilliseconds = allocationTimeoutInMilliseconds;
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, Guid requestId)
        {
            var pending = (PendingResRequest<QueuedEventsResponse>) pendingRequest;

            var msg = new NetMQMessage();
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(ResCommands.AcknowledgeQueue);
            msg.Append(requestId.ToByteArray());

            msg.Append(_context);
            msg.Append(_queueId);
            msg.Append(_subscriberId);
            msg.Append(_allocationId.ToNetMqFrame());
            msg.Append(_allocationBatchSize.ToNetMqFrame());
            msg.Append(_allocationTimeoutInMilliseconds.ToNetMqFrame());

            socket.SendMultipartMessage(msg);

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

                var queueContext = m.Pop().ConvertToString();
                var queueId = m.Pop().ConvertToString();
                var subscriberId = m.Pop().ConvertToString();
                var time = m.PopDateTime();
                var allocationId = m.PopNullableInt64();
                var count = m.PopInt32();

                var events = new EventInStorage[count];

                for (var i = 0; i < count; i++)
                {
                    var id = new Guid(m.Pop().ToByteArray());
                    var streamId = m.Pop().ConvertToString();
                    var context = m.Pop().ConvertToString();
                    var sequence = m.PopInt64();
                    var timestamp = m.PopDateTime();
                    var type = m.PopString();
                    var headers = m.PopStringOrNull();
                    var body = m.PopString();

                    events[i] = new EventInStorage(context, streamId, sequence, type, id, headers, body, timestamp);
                }

                var result = new QueuedEventsResponse(queueContext, queueId, subscriberId, time, allocationId, events);
                pending.SetResult(result);
            };
        }
    }
}