using System;

namespace Res.Client.Internal
{
    public interface ResGateway : IDisposable
    {
        bool ProcessResponse();
        void SendRequest(PendingResRequest pendingResRequest);
        void KillRequestsThatHaveTimedOut();
    }
}