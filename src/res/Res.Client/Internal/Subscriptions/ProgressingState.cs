using Common.Logging;
using Res.Client.Internal.Subscriptions.Messages;

namespace Res.Client.Internal.Subscriptions
{
    public class ProgressingState : SubscriptionProcessState
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public SubscriptionProcessState Work(SubscriptionState state)
        {
            while (state.CancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    if(!state.LastEventTime.HasValue)
                        return new FetchingState();

                    Log.Debug("[ProgressingState] Progressing subscription.");

                    state.Acceptor.ProgressAsync(new[] {new ProgressSubscriptionEntry(state.SubscriptionId, state.LastEventTime.Value)},
                        state.DefaultRequestTimeOut).Wait(state.CancellationToken);

                    Log.Debug("[ProgressingState] Subscription progressed.");

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