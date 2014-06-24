namespace Res.Client
{
    public class EventsForStream
    {
        public string Context { get; private set; }
        public string Stream { get; private set; }
        public EventInStorage[] Events { get; private set; }

        public EventsForStream(string context, string stream, EventInStorage[] events)
        {
            Context = context;
            Stream = stream;
            Events = events;
        }
    }
}