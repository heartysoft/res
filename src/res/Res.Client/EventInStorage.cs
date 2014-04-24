using System;

namespace Res.Client
{
    public class EventInStorage
    {
        public string Context { get; private set; }
        public Guid EventId { get; private set; }
        public string TypeTag { get; private set; }
        public string Headers { get; private set; }
        public string Body { get; private set; }
        public DateTime Timestamp { get; private set; }

        public EventInStorage(string context, string typeTag, Guid eventId, string headers, string body, DateTime timestamp)
        {
            Context = context;
            TypeTag = typeTag;
            EventId = eventId;
            Headers = headers;
            Body = body;
            Timestamp = timestamp;
        }
    }
}