using System;
using NetMQ;

namespace Res.Client.Internal.Subscriptions.Messages
{
    public class FetchEventsRequest : ResRequest
    {
        public FetchEventParameters[] FetchEventParameters { get; private set; }

        public FetchEventsRequest(FetchEventParameters[] fetchEvents)
        {
            FetchEventParameters = fetchEvents;
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, string requestId)
        {
            throw new NotImplementedException();
        }
    }
}