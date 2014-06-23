using System;
using System.Threading.Tasks;
using Res.Client.Internal.Commits.Messages;

namespace Res.Client.Internal.Commits
{
    public class CommitRequestAcceptor
    {
        private readonly MultiWriterSingleReaderBuffer _buffer;

        public CommitRequestAcceptor(MultiWriterSingleReaderBuffer buffer)
        {
            _buffer = buffer;
        }

        public Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion, TimeSpan timeout)
        {
            var commitRequest = new CommitRequest(context, stream, events, expectedVersion);
            var task = _buffer.Enqueue<CommitResponse>(commitRequest, DateTime.Now.Add(timeout));
            return task;
        }
    }

    
}