using System;
using System.Threading.Tasks;

namespace Res.Client
{
    public class DummyResClient : ResClient
    {
        public Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion)
        {
            return Task.FromResult(default(CommitResponse));
        }

        public Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion, TimeSpan timeout)
        {
            return Task.FromResult(default(CommitResponse));
        }
    }
}