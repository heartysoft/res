using System.Linq;
using Res.Client.Internal.Subscriptions.Messages;

namespace Res.Client.Internal.Subscriptions
{
    public class ProcessingState : SubscriptionProcessState
    {
        private readonly FetchEventsResponse _events;
        
        public ProcessingState(FetchEventsResponse events)
        {
            _events = events;
        }

        public SubscriptionProcessState Work(SubscriptionState state)
        {
            if (_events.Events.Length > 0)
            {
                var subscribedEvents = new SubscribedEvents(_events.Events);
                state.Handler(subscribedEvents);

                subscribedEvents.Completed.Wait(state.CancellationToken);
                state.LastEventTime = _events.Events.Last().Timestamp;
                state.EventIdsForLastEventTime = _events.Events.Where(x => x.Timestamp == state.LastEventTime).Select(x => x.EventId).ToArray();

                return new ProgressingState();
            }
            else
            {
                return new FetchingState();                
            }
        }
    }
}