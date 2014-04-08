using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Res.Client
{
    public class RequestAcceptor
    {
        private readonly MultiWriterSingleReaderBuffer _buffer;

        public RequestAcceptor(MultiWriterSingleReaderBuffer buffer)
        {
            _buffer = buffer;
        }

        public Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion)
        {
            var timeout = TimeSpan.FromSeconds(10);
            var commitRequest = new CommitRequest(context, stream, events, expectedVersion);
            var task = _buffer.Enqueue<CommitResponse>(commitRequest, DateTime.Now.Add(timeout));
            return task;
        }
    }
}