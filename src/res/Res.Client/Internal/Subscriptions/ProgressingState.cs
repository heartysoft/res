using Res.Client.Internal.Subscriptions.Messages;

namespace Res.Client.Internal.Subscriptions
{
    public class ProgressingState : SubscriptionProcessState
    {
        
        public SubscriptionProcessState Work(SubscriptionState state)
        {
            while (state.CancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    if(!state.LastEventTime.HasValue)
                        return new FetchingState();

                    state.Acceptor.ProgressAsync(new[] {new ProgressSubscriptionEntry(state.SubscriptionId, state.LastEventTime.Value)},
                        state.DefaultRequestTimeOut).Wait(state.CancellationToken);

                    return new FetchingState();
                }
                catch (RequestTimedOutPendingSendException)
                {
                }
            }
            
            return new InvalidSubscriptionProcessState();
        }
    }
}