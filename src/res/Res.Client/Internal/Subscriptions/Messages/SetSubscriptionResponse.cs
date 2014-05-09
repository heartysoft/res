using System;

namespace Res.Client.Internal.Subscriptions.Messages
{
    public class SetSubscriptionResponse : ResResponse
    {
        public SubscriptionSet[] SubscriptionsSet { get; private set; }

        public SetSubscriptionResponse(SubscriptionSet[] subscriptions)
        {
            SubscriptionsSet = subscriptions;
        }

        public class SubscriptionSet
        {
            public long SubscriptionId { get; private set; }
            public DateTime ResetTo { get; private set; }

            public SubscriptionSet(long subscriptionId, DateTime resetTo)
            {
                SubscriptionId = subscriptionId;
                ResetTo = resetTo;
            }
        }
    }
}