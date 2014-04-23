namespace Res.Core.Storage
{
    public class SubscribeResponse
    {
        public int SubscriptionRequestId { get; private set; }
        public long SubscriptionId { get; set; }

        public SubscribeResponse(int subscriptionRequestId, long subscriptionId)
        {
            SubscriptionRequestId = subscriptionRequestId;
            SubscriptionId = subscriptionId;
        }
    }
}