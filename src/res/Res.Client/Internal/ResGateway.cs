using System;

namespace Res.Client.Internal
{
    public interface ResGateway
    {
        bool ProcessResponse();
        void SendRequest(PendingResRequest pendingResRequest);
        void KillRequestsThatHaveTimedOut();
        void Shutdown();
    }
}