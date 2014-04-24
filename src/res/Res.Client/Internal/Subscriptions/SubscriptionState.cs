using System;
using System.Threading;

namespace Res.Client.Internal.Subscriptions
{
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
    }
}