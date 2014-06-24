namespace Res.Client.Internal.Queries.Messages
{
    public class QueryEventsForStreamResponse : ResResponse
    {
        public string Context { get; private set; }
        public string Stream { get; private set; }
        public EventInStorage[] Events { get; private set; }

        public QueryEventsForStreamResponse(string context, string stream, EventInStorage[] events)
        {
            Context = context;
            Stream = stream;
            Events = events;
        }
    }
}