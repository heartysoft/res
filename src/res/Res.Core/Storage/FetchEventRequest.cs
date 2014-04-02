using System;

namespace Res.Core.Storage
{
    public class FetchEventRequest
    {
        public Guid EventId { get; private set; }
        public string Context { get; private set; }
        public string Stream { get; private set; }
        public Guid RequestId { get; private set; }

        public FetchEventRequest(Guid eventId, string context, string stream)
        {
            EventId = eventId;
            Context = context;
            Stream = stream;
            RequestId = Guid.NewGuid();
        }
    }
}