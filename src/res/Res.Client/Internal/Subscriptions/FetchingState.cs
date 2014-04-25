using System.Linq;
using Res.Client.Internal.Subscriptions.Messages;

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
                        state.DefaultRequestTimeOut).GetAwaiter().GetResult();

                    if (state.LastEventTime.HasValue && state.EventIdsForLastEventTime != null)
                    {
                        var newEvents =
                            result.Events.Where(x => x.Timestamp >= state.LastEventTime && state.EventIdsForLastEventTime.Contains(x.EventId) == false)
                                .ToArray();

                        result = new FetchEventsResponse(newEvents);
                    }

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