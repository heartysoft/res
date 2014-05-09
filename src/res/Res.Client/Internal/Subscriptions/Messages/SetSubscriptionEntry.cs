using System;

namespace Res.Client.Internal.Subscriptions.Messages
{
    public class SetSubscriptionEntry
    {
        public long SubscriptionId { get; private set; }
        public DateTime ResetTo { get; private set; }
        public DateTime LastEventTime { get; private set; }

        public SetSubscriptionEntry(long subscriptionId, DateTime resetTo, DateTime lastEventTime)
        {
            SubscriptionId = subscriptionId;
            ResetTo = resetTo;
            LastEventTime = lastEventTime;
        }
    }
}