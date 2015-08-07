using System;
using NetMQ;

namespace Res.Client.Internal
{
    public interface ResRequest
    {
        Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, Guid requestId);
    }
}