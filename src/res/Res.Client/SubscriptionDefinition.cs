using System;

namespace Res.Client
{
    public class SubscriptionDefinition
    {
        public string Context { get; private set; }
        public string Filter { get; private set; }

        public SubscriptionDefinition(string context, string filter)
        {
            Context = context;
            Filter = filter;
        }
    }
}