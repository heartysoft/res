using System;
using System.Threading.Tasks;
using Res.Client.Internal;

namespace Res.Client
{
    public class ThreadsafeResClient : ResClient
    {
        public Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion, TimeSpan timeout)
        {
            return ResEngine.CommitAsync(context, stream, events, expectedVersion, timeout);
        }

        public Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion)
        {
            return ResEngine.CommitAsync(context, stream, events, expectedVersion);
        }
    }
}