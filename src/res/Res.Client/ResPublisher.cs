using System;
using System.CodeDom;
using System.Threading.Tasks;

namespace Res.Client
{
    public interface ResPublisher
    {
        Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion);
        Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion, TimeSpan timeout);
    }
}