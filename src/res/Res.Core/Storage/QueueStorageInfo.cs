namespace Res.Core.Storage
{
    public class QueueStorageInfo
    {
        public string QueueId { get; private set; }
        public string Context { get; private set; }
        public string Filter { get; private set; }
        public long NextMarker { get; private set; }

        public QueueStorageInfo(string context, string queueId, string filter, long nextMarker)
        {
            QueueId = queueId;
            Context = context;
            Filter = filter;
            NextMarker = nextMarker;
        }

        public bool Matches(string context, string queueId, string filter)
        {
            return QueueId.Equals(queueId) && Context.Equals(context) && Filter.Equals(filter);
        }

        public bool MatchesQueue(string queueId)
        {
            return QueueId.Equals(queueId);
        }

        public bool MatchesEvent(EventInStorage eventInStorage)
        {
            if (!MatchesContextAndFilter(eventInStorage.Context, eventInStorage.Stream)) return false;
            if (eventInStorage.GlobalSequence < NextMarker) return false;

            return true;
        }

        public QueueStorageInfo WithNextMarker(long value)
        {
            return new QueueStorageInfo(Context, QueueId, Filter, value);
        }

        public bool MatchesContextAndFilter(string context, string stream)
        {
            if (!Context.Equals(context)) return false;
            if (Filter != "*" && stream.StartsWith(Filter) == false) return false;
            return true;
        }
    }
}