using System;
using System.Threading.Tasks;
using Res.Client.Internal;
using Res.Client.Internal.Commits;

namespace Res.Client
{
    public class ThreadsafeResClient : ResClient
    {
        private readonly CommitRequestAcceptor _acceptor;
        private readonly TimeSpan _defaultTimeout;

        public ThreadsafeResClient(CommitRequestAcceptor acceptor, TimeSpan defaultTimeout)
        {
            _acceptor = acceptor;
            _defaultTimeout = defaultTimeout;
        }

        public Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion, TimeSpan timeout)
        {
            return _acceptor.CommitAsync(context, stream, events, expectedVersion, timeout);
        }

        public Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion)
        {
            return CommitAsync(context, stream, events, expectedVersion, _defaultTimeout);
        }
    }
}