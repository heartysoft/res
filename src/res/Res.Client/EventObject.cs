using System;
using System.Collections.Generic;

namespace Res.Client
{
    public class EventObject
    {
        private readonly Guid _eventId;
        private readonly Dictionary<string, string> _headers;
        private readonly object _body;
        private readonly DateTime _timestamp;

        public EventObject(Guid eventId, Dictionary<string, string> headers, object body, DateTime timestamp)
        {
            _eventId = eventId;
            _headers = headers;
            _body = body;
            _timestamp = timestamp;
        }

        public EventData ToEventData(Func<object, string> serialiser, TypeTagResolver typeTagResolver)
        {
            return new EventData(typeTagResolver.GetTagFor(_body), _eventId, _headers == null? "{}" : serialiser(_headers), serialiser(_body), _timestamp);
        }
    }
}