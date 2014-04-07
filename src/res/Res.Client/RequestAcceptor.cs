using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Res.Client
{
    public interface ResResult
    {
    }

    public interface ResRequest
    {

    }

    public class CommitResult : ResResult
    {
        public Guid CommitId { get; set; }

        public CommitResult(Guid commitId)
        {
            CommitId = commitId;
        }
    }

    public class CommitRequest : ResRequest
    {
        private readonly string _context;
        private readonly string _stream;
        private readonly IEnumerable<EventData> _events;

        public CommitRequest(string context, string stream, IEnumerable<EventData> events)
        {
            _context = context;
            _stream = stream;
            _events = events;
        }
    }

    public class EventData
    {
        public Guid EventId { get; private set; }
        public string TypeTag { get; private set; }
        public string Headers { get; private set; }
        public string Body { get; private set; }

        public EventData(string typeTag, Guid eventId, string headers, string body)
        {
            TypeTag = typeTag;
            EventId = eventId;
            Headers = headers;
            Body = body;
        }
    }

    public class RequestAcceptor
    {
        private readonly MultiWriterSingleReaderBuffer _buffer;

        public RequestAcceptor(MultiWriterSingleReaderBuffer buffer)
        {
            _buffer = buffer;
        }

        private Task<CommitResult> CommitAsync(string context, string stream, IEnumerable<EventData> events)
        {
            var commitRequest = new CommitRequest(context, stream, events);
            var task = _buffer.Enqueue<CommitResult>(commitRequest);
            return task;
        }
    }

    public interface ResGateway
    {
        void ProcessResponse();
    }

}