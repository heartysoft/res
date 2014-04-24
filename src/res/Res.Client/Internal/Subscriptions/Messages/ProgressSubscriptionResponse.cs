namespace Res.Client.Internal.Subscriptions.Messages
{
    public class ProgressSubscriptionResponse : ResResponse
    {
        public long[] SubscriptionIds { get; private set; }

        public ProgressSubscriptionResponse(long[] subscriptionIds)
        {
            SubscriptionIds = subscriptionIds;
        }
    }
}