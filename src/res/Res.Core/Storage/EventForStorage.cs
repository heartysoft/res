using System;

namespace Res.Core.Storage
{
    public class EventForStorage
    {
        public Guid EventId { get; private set; }
        public long Sequence { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string TypeKey { get; private set; }
        public string Body { get; private set; }
        public string Headers { get; private set; }

        public EventForStorage(Guid eventId, long sequence, DateTime timestamp, string typeKey, string body, string headers)
        {
            EventId = eventId;
            Sequence = sequence;
            Timestamp = timestamp;
            TypeKey = typeKey;
            Body = body;
            Headers = headers;
        }
    }
}