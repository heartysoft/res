using System;
using System.Globalization;
using NetMQ;
using Res.Client.Exceptions;
using Res.Protocol;

namespace Res.Client.Internal.Queues.Messages
{
    public class AcknowledgeQueueAndFetchNextRequest : ResRequest
    {
        private readonly string _queueId;
        private readonly string _subscriberId;
        private readonly long? _allocationId;
        private readonly int _allocationBatchSize;
        private readonly int _allocationTimeoutInMilliseconds;

        public AcknowledgeQueueAndFetchNextRequest(string queueId, string subscriberId, long? allocationId, int allocationBatchSize, int allocationTimeoutInMilliseconds)
        {
            _queueId = queueId;
            _subscriberId = subscriberId;
            _allocationId = allocationId;
            _allocationBatchSize = allocationBatchSize;
            _allocationTimeoutInMilliseconds = allocationTimeoutInMilliseconds;
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, string requestId)
        {
            var pending = (PendingResRequest<QueuedEventsResponse>) pendingRequest;

            var msg = new NetMQMessage();
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(ResCommands.AcknowledgeQueue);
            msg.Append(requestId);

            msg.Append(_queueId);
            msg.Append(_subscriberId);

            if(_allocationId.HasValue)
                msg.Append(_allocationId.Value.ToString(CultureInfo.InvariantCulture));
            else
                msg.AppendEmptyFrame();
            
            msg.Append(_allocationBatchSize.ToString(CultureInfo.InvariantCulture));
            msg.Append(_allocationTimeoutInMilliseconds.ToString(CultureInfo.InvariantCulture));

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

                var queueId = m.Pop().ConvertToString();
                var subscriberId = m.Pop().ConvertToString();
                var time = DateTime.FromBinary(long.Parse(m.Pop().ConvertToString()));

                long? allocationId = null;
                var allocationFrame = m.Pop();

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

                var result = new QueuedEventsResponse(queueId, subscriberId, time, allocationId, events);
                pending.SetResult(result);
            };
        }
    }
}