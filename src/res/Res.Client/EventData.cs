using System;

namespace Res.Client
{
    public class EventData
    {
        public Guid EventId { get; private set; }
        public string TypeTag { get; private set; }
        public string Headers { get; private set; }
        public string Body { get; private set; }
        public DateTime Timestamp { get; private set; }

        public EventData(string typeTag, Guid eventId, string headers, string body, DateTime timestamp)
        {
            TypeTag = typeTag;
            EventId = eventId;
            Headers = headers;
            Body = body;
            Timestamp = timestamp;
        }
    }
}