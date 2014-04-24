using System;
using NetMQ;

namespace Res.Client.Internal.Subscriptions.Messages
{
    public class SubscribeRequest : ResRequest
    {
        public SubscriptionDefinition[] Subscriptions { get; private set; }

        public SubscribeRequest(SubscriptionDefinition[] subscriptions)
        {
            Subscriptions = subscriptions;
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, string requestId)
        {
            throw new NotImplementedException();
        }
    }
}