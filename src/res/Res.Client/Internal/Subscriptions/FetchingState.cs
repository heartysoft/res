namespace Res.Client.Internal.Subscriptions
{
    public class FetchingState : SubscriptionProcessState
    {
        public SubscriptionProcessState Work(SubscriptionState state)
        {
            while (state.CancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    var result = state.Acceptor.FetchEventsAsync(new[] {new FetchEventParameters(state.SubscriptionId, state.FetchEventsBatchSize)},
                        state.DefaultRequestTimeOut).Result;

                    if (result.Events.Length == 0)
                        return new WaitingState();

                    return new ProcessingState(result);
                }
                catch (RequestTimedOutPendingSendException)
                {
                }
            }

            return new InvalidSubscriptionProcessState();
        }
    }
}