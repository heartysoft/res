using System;
using System.Threading.Tasks;
using Res.Protocol;

namespace Res.Client
{
    public interface ResEventPublisher
    {
        Task<CommitResponse> Publish(string stream, EventObject[] events, long expectedVersion = ExpectedVersion.Any,
            TimeSpan? timeout = null);
    }
}