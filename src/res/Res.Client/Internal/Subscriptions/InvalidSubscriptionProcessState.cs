using Common.Logging;

namespace Res.Client.Internal.Subscriptions
{
    public class InvalidSubscriptionProcessState : SubscriptionProcessState
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public SubscriptionProcessState Work(SubscriptionState state)
        {
            Log.InfoFormat("Invalid state reached for subscription {0}.", state.SubscriptionId);
            return null;
        }
    }
}