using System;
using System.Threading.Tasks;
using Res.Client.Internal.Queries.Messages;

namespace Res.Client.Internal.Queries
{
    public class QueryRequestAcceptor
    {
        private readonly MultiWriterSingleReaderBuffer _buffer;

        public QueryRequestAcceptor(MultiWriterSingleReaderBuffer buffer)
        {
            _buffer = buffer;
        }


        public Task<QueryEventsForStreamResponse> QueryByStream(
            string context, string stream, long minVersion,
            TimeSpan timeout)
        {
            return QueryByStream(context, stream, minVersion, null, timeout);
        }

        public Task<QueryEventsForStreamResponse> QueryByStream(
            string context, string stream, TimeSpan timeout)
        {
            return QueryByStream(context, stream, 0, null, timeout);
        }

        public Task<QueryEventsForStreamResponse> QueryByStream(
            string context, string stream, long minVersion,
            long? maxVersion, TimeSpan timeout)
        {
            var request = new QueryEventsForStreamRequest(context, stream, minVersion, maxVersion);
            var task = _buffer.Enqueue<QueryEventsForStreamResponse>(request, DateTime.Now.Add(timeout));
            return task;
        }
    }
}