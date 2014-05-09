using System;
using System.Globalization;
using System.Linq;
using System.Text;
using NetMQ;
using Res.Client.Exceptions;
using Res.Protocol;

namespace Res.Client.Internal.Subscriptions.Messages
{
    public class SetSubscriptionTimesRequest : ResRequest
    {
        private readonly SetSubscriptionEntry[] _subscriptions;

        public SetSubscriptionTimesRequest(SetSubscriptionEntry[] subscriptions)
        {
            _subscriptions = subscriptions;
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, string requestId)
        {
            var pending = (PendingResRequest<SetSubscriptionTimesResponse>)pendingRequest;

            var msg = new NetMQMessage();
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(ResCommands.SetSubscriptions);
            msg.Append(requestId);

            msg.Append(_subscriptions.Length.ToString(CultureInfo.InvariantCulture));

            foreach (var sub in _subscriptions)
            {
                msg.Append(sub.SubscriberId);
                msg.Append(sub.Context);
                msg.Append(sub.Filter);
                msg.Append(sub.SetTo.ToBinary().ToString(CultureInfo.InvariantCulture));
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

                if (command != ResCommands.SubscriptionsSet)
                    pending.SetException(new UnsupportedCommandException(command));

                var count = int.Parse(m.Pop().ConvertToString());
                var subs = new SetSubscriptionTimesResponse.SubscriptionSet[count];

                bool anyError = false;

                for (var i = 0; i < count; i++)
                {
                    var error = m.Pop();
                    if (error.BufferSize == 0)
                        subs[i] = new SetSubscriptionTimesResponse.SubscriptionSet(true, null);
                    else
                    {

                        subs[i] = new SetSubscriptionTimesResponse.SubscriptionSet(false, error.ConvertToString());
                        anyError = true;
                    }
                }

                var response = new SetSubscriptionTimesResponse(subs);
                
                if(!anyError)
                    pending.SetResult(response);
                else 
                    pending.SetException(new SetSubscriptionStorageException(response));
            };
        }

        public class SetSubscriptionStorageException : Exception
        {
            public SetSubscriptionStorageException(SetSubscriptionTimesResponse response)
                : base(getMessage(response))
            {
            }

            private static string getMessage(SetSubscriptionTimesResponse response)
            {
                var sb = new StringBuilder();
                sb.AppendLine("One or more subscriptions could not be set.");
                sb.AppendLine("Errors: ");
                var failures = response.SubscriptionsSet.Where(x => !x.Successful);
                foreach (var subscriptionSet in failures)
                {
                    sb.AppendFormat("- {0}", subscriptionSet.ErrorMessage);
                    sb.AppendLine();
                }

                return sb.ToString();
            }
        }
    }
}