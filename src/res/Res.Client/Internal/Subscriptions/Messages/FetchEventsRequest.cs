using System;
using System.Globalization;
using NetMQ;
using Res.Client.Exceptions;
using Res.Protocol;

namespace Res.Client.Internal.Subscriptions.Messages
{
    public class FetchEventsRequest : ResRequest
    {
        private readonly FetchEventParameters[] _fetchEvents;

        public FetchEventsRequest(FetchEventParameters[] fetchEvents)
        {
            _fetchEvents = fetchEvents;
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, string requestId)
        {
            var pending = (PendingResRequest<FetchEventsResponse>)pendingRequest;

            var msg = new NetMQMessage();
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(ResCommands.FetchEvents);
            msg.Append(requestId);

            msg.Append(_fetchEvents.Length.ToString(CultureInfo.InvariantCulture));

            foreach (var fe in _fetchEvents)
            {
                msg.Append(fe.SubscriptionId.ToString(CultureInfo.InvariantCulture));
                msg.Append(fe.SuggestedCount.ToString(CultureInfo.InvariantCulture));
            }

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

                if (command != ResCommands.EventsFetched)
                    pending.SetException(new UnsupportedCommandException(command));

                var subscriptionId = long.Parse(msg.Pop().ConvertToString());
                var count = int.Parse(msg.Pop().ConvertToString());

                var events = new EventInStorage[count];

                for (var i = 0; i < count; i++)
                {
                    var id = new Guid(m.Pop().ToByteArray());
                    var streamId = m.Pop().ConvertToString();
                    var context = m.Pop().ConvertToString();
                    var sequence = long.Parse(msg.Pop().ConvertToString());
                    var timestamp = DateTime.FromBinary(long.Parse(msg.Pop().ConvertToString()));
                    var type = msg.Pop().ConvertToString();
                    var headers = msg.Pop().ConvertToString();
                    var body = msg.Pop().ConvertToString();

                    events[i] = new EventInStorage(context, streamId, sequence, type, id, headers, body, timestamp);
                }
                
                var result = new FetchEventsResponse(events);
                pending.SetResult(result);
            };
        }
    }
}