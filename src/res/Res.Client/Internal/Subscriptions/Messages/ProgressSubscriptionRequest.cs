using System;
using NetMQ;

namespace Res.Client.Internal.Subscriptions.Messages
{
    public class ProgressSubscriptionRequest : ResRequest
    {
        public ProgressSubscriptionEntry[] Subscriptions { get; private set; }

        public ProgressSubscriptionRequest(ProgressSubscriptionEntry[] subscriptions)
        {
            Subscriptions = subscriptions;
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, string requestId)
        {
            throw new NotImplementedException();
        }
    }
}