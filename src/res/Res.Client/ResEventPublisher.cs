using System;
using System.Threading.Tasks;

namespace Res.Client
{
    public interface ResEventPublisher
    {
        Task<CommitResponse> Publish(string stream, object[] events, long expectedVersion = ExpectedVersion.Any,
            TimeSpan? timeout = null);
    }
}