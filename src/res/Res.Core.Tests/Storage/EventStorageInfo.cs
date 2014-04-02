using System;

namespace Res.Core.Tests.Storage
{
    public class EventStorageInfo
    {
        public Guid EventId { get; set; }
        public int Sequence { get; set; }
        public DateTime Timestamp { get; set; }
        public string TypeKey { get; set; }
        public string Body { get; set; }
        public string Header { get; set; }

        public EventStorageInfo(Guid eventId, int sequence, DateTime timestamp, string typeKey, string body, string header)
        {
            EventId = eventId;
            Sequence = sequence;
            Timestamp = timestamp;
            TypeKey = typeKey;
            Body = body;
            Header = header;
        }
    }
}