using System;

namespace Res.Client.Internal.Subscriptions.Messages
{
    public class SetSubscriptionTimesResponse : ResResponse
    {
        public SubscriptionSet[] SubscriptionsSet { get; private set; }

        public SetSubscriptionTimesResponse(SubscriptionSet[] subscriptions)
        {
            SubscriptionsSet = subscriptions;
        }

        public class SubscriptionSet
        {
            public bool Successful { get; private set; }
            public string ErrorMessage { get; private set; }

            public SubscriptionSet(bool successful, string errorMessage)
            {
                Successful = successful;
                ErrorMessage = errorMessage;
            }
        }
    }
}