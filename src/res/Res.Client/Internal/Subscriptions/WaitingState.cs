using System.Threading.Tasks;

namespace Res.Client.Internal.Subscriptions
{
    public class WaitingState : SubscriptionProcessState
    {
        public SubscriptionProcessState Work(SubscriptionState state)
        {
            Task.Delay(state.WaitBeforeRetryingFetch, state.CancellationToken);
            return new FetchingState();
        }
    }
}