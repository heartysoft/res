using System;
using NetMQ;

namespace Res.Core.Storage
{
    public class EventInStorage
    {
        public Guid EventId { get; private set; }
        public string Context { get; private set; }
        public string Stream { get; private set; }
        public long Sequence { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string TypeKey { get; private set; }
        public string Body { get; private set; }
        public string Headers { get; private set; }
        public long GlobalSequence { get; private set; }

        public EventInStorage(Guid eventId, string context, string stream, long sequence, long globalSequence, DateTime timestamp, string typeKey, string body,
            string headers)
        {
            EventId = eventId;
            Context = context;
            Stream = stream;
            Sequence = sequence;
            GlobalSequence = globalSequence;
            Timestamp = timestamp;
            TypeKey = typeKey;
            Body = body;
            Headers = headers;
        }
    }
}