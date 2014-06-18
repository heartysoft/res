using System;

namespace Res.Client
{
    public class QueuedEvents
    {
        public string QueueId { get; private set; }
        public string SubscriberId { get; private set; }
        public EventInStorage[] Events { get; private set; }
        public int StartMarker { get; private set; }
        public int EndMarker { get; private set; }
        public DateTime TimeOfResponse { get; private set; }
        public ResQueue ResQueue { get; private set; }

        public QueuedEvents(string queueId, string subscriberId, EventInStorage[] events, int startMarker, int endMarker, DateTime timeOfResponse, ResQueue resQueue)
        {
            QueueId = queueId;
            SubscriberId = subscriberId;
            Events = events;
            StartMarker = startMarker;
            EndMarker = endMarker;
            TimeOfResponse = timeOfResponse;
            ResQueue = resQueue;
        }
    }
}