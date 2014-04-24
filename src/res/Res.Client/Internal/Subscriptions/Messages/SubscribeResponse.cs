namespace Res.Client.Internal.Subscriptions.Messages
{
    public class SubscribeResponse : ResResponse
    {
        public long[] SubscriptionIds { get; private set; }

        public SubscribeResponse(long[] subscriptionIds)
        {
            SubscriptionIds = subscriptionIds;
        }
    }
}