using System;
using System.Globalization;
using System.Runtime.Remoting.Messaging;
using NetMQ;
using Res.Client.Exceptions;
using Res.Protocol;

namespace Res.Client.Internal.Subscriptions.Messages
{
    public class SubscribeRequest : ResRequest
    {
        private readonly string _subscriberId;
        private readonly SubscriptionDefinition[] _subscriptions;
        private readonly DateTime _startTime;

        public SubscribeRequest(string subscriberId, SubscriptionDefinition[] subscriptions, DateTime startTime)
        {
            _subscriberId = subscriberId;
            _subscriptions = subscriptions;
            _startTime = startTime;
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, string requestId)
        {
            var pending = (PendingResRequest<SubscribeResponse>) pendingRequest;

            var msg = new NetMQMessage();
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(ResCommands.RegisterSubscriptions);
            msg.Append(requestId);

            msg.Append(_subscriberId);
            msg.Append(_subscriptions.Length.ToString(CultureInfo.InvariantCulture));

            foreach (var sub in _subscriptions)
            {
                msg.Append(sub.Context);
                msg.Append(sub.Filter);
                msg.Append(_startTime.ToBinary().ToString(CultureInfo.InvariantCulture));
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

                if (command != ResCommands.SubscribeResponse)
                    pending.SetException(new UnsupportedCommandException(command));

                int count = int.Parse(m.Pop().ConvertToString());
                var subscriptionIds = new long[count];

                for (int i = 0; i < count; i++)
                    subscriptionIds[i] = long.Parse(m.Pop().ConvertToString());
                
                var result = new SubscribeResponse(subscriptionIds);

                pending.SetResult(result);
            };
        }
    }
}