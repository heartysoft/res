namespace Res.Client.Internal.Subscriptions.Messages
{
    public class FetchEventsResponse : ResResponse
    {
        public EventInStorage[] Events { get; private set; }

        public FetchEventsResponse(EventInStorage[] events)
        {
            Events = events;
        }
    }
}