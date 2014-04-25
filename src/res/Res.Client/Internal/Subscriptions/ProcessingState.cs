using System.Linq;
using Common.Logging;
using Res.Client.Internal.Subscriptions.Messages;

namespace Res.Client.Internal.Subscriptions
{
    public class ProcessingState : SubscriptionProcessState
    {
        private readonly FetchEventsResponse _events;
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        
        public ProcessingState(FetchEventsResponse events)
        {
            _events = events;
        }

        public SubscriptionProcessState Work(SubscriptionState state)
        {
            if (_events.Events.Length > 0)
            {
                Log.Debug("[ProcessingState] Got new events. Pushing to handler.");
                var subscribedEvents = new SubscribedEvents(_events.Events);
                state.Handler(subscribedEvents);

                subscribedEvents.Completed.Wait(state.CancellationToken);
                
                Log.Debug("[ProcessingState] Handler completed.");

                state.LastEventTime = _events.Events.Last().Timestamp;
                state.EventIdsForLastEventTime = _events.Events.Where(x => x.Timestamp == state.LastEventTime).Select(x => x.EventId).ToArray();

                Log.Debug("[ProcessingState] Transitioning to ProgressingState.");

                return new ProgressingState();
            }
            else
            {
                return new FetchingState();                
            }
        }
    }
}