namespace Res.Client.Internal.Subscriptions
{
    public class FetchEventParameters
    {
        public long SubscriptionId { get; private set; }
        public int SuggestedCount { get; private set; }

        public FetchEventParameters(long subscriptionId, int suggestedCount)
        {
            SubscriptionId = subscriptionId;
            SuggestedCount = suggestedCount;
        }
    }
}