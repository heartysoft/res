namespace Res.Client.Internal.Subscriptions
{
    public interface SubscriptionProcessState
    {
        SubscriptionProcessState Work(SubscriptionState state);
    }
}