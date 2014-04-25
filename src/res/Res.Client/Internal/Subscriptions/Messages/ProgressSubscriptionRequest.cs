using System;
using System.Globalization;
using NetMQ;
using Res.Client.Exceptions;
using Res.Protocol;

namespace Res.Client.Internal.Subscriptions.Messages
{
    public class ProgressSubscriptionRequest : ResRequest
    {
        private readonly ProgressSubscriptionEntry[] _subscriptions;

        public ProgressSubscriptionRequest(ProgressSubscriptionEntry[] subscriptions)
        {
            _subscriptions = subscriptions;
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, string requestId)
        {
            var pending = (PendingResRequest<ProgressSubscriptionResponse>)pendingRequest;

            var msg = new NetMQMessage();
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(ResCommands.ProgressSubscriptions);
            msg.Append(requestId);

            msg.Append(_subscriptions.Length.ToString(CultureInfo.InvariantCulture));

            foreach (var sub in _subscriptions)
            {
                msg.Append(sub.SubscriptionId.ToString(CultureInfo.InvariantCulture));
                msg.Append(sub.LastEventTime.ToBinary().ToString(CultureInfo.InvariantCulture));
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

                if (command != ResCommands.SubscriptionsProgressed)
                    pending.SetException(new UnsupportedCommandException(command));

                var count = int.Parse(m.Pop().ConvertToString());
                var subIds = new long[count];

                for (var i = 0; i < count; i++)
                    subIds[i] = long.Parse(m.Pop().ConvertToString());

                var response = new ProgressSubscriptionResponse(subIds);
                pending.SetResult(response);
            };
        }
    }
}