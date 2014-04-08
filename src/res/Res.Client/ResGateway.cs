using System;
using System.Net.Sockets;
using System.Runtime.Remoting;

namespace Res.Client
{
    public interface ResGateway : IDisposable
    {
        bool ProcessResponse();
        void SendRequest(PendingResRequest pendingResRequest);
        void KillRequestsThatHaveTimedOut();
    }
}