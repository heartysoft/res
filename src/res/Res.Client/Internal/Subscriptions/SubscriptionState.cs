using System;
using System.Threading;

namespace Res.Client.Internal.Subscriptions
{
    /// <summary>
    /// State for the Subscription state machine. Yes, I know this is a bit of a dump at the moment. 
    /// Coupling the state to one place is "initially" simpler than drawing out a proper model, given that
    /// a) few states, and b) this is only used in one state machine.
    /// Also, the state machine runs strictly sunchronously...no threading issues.
    /// </summary>
    public class SubscriptionState
    {
        public DateTime? LastEventTime { get; set; }
        public Action<SubscribedEvents> Handler { get; set; }
        public TimeSpan DefaultRequestTimeOut { get; set; }
        public long SubscriptionId { get; set; }
        public int FetchEventsBatchSize { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public SubscriptionRequestAcceptor Acceptor { get; set; }
        public TimeSpan WaitBeforeRetryingFetch { get; set; }
        public Guid[] EventIdsForLastEventTime { get; set; }
    }
}