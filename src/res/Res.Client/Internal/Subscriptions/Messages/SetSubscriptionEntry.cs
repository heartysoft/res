using System;

namespace Res.Client.Internal.Subscriptions.Messages
{
    public class SetSubscriptionEntry
    {
        public string SubscriberId { get; private set; }
        public string Context { get; private set; }
        public string Filter { get; private set; }
        public DateTime SetTo { get; private set; }

        public SetSubscriptionEntry(string subscriberId, string context, string filter, DateTime setTo)
        {
            SubscriberId = subscriberId;
            Context = context;
            Filter = filter;
            SetTo = setTo;
        }
    }
}