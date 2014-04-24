using System;

namespace Res.Client
{
    public class SubscriptionDefinition
    {
        public string Context { get; private set; }
        public string Filter { get; private set; }
        public DateTime StartTime { get; private set; }

        public SubscriptionDefinition(string context, string filter, DateTime startTime)
        {
            Context = context;
            Filter = filter;
            StartTime = startTime;
        }
    }
}