using System;

namespace Res.Client.Internal.Subscriptions.Messages
{
    public class ProgressSubscriptionEntry
    {
        public long SubscriptionId { get; private set; }
        public DateTime LastEventTime { get; private set; }

        public ProgressSubscriptionEntry(long subscriptionId, DateTime lastEventTime)
        {
            SubscriptionId = subscriptionId;
            LastEventTime = lastEventTime;
        }
    }
}