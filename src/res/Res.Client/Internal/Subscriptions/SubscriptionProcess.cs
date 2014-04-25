using System;
using System.Threading;

namespace Res.Client.Internal.Subscriptions
{
    public class SubscriptionProcess
    {
        private readonly SubscriptionState _state;

        public SubscriptionProcess(long subscriptionId, Action<SubscribedEvents> handler, SubscriptionRequestAcceptor acceptor, TimeSpan timeout, CancellationToken token)
        {
            _state = new SubscriptionState
            {
                CancellationToken = token,
                DefaultRequestTimeOut = timeout,
                FetchEventsBatchSize = 32,
                Handler = handler,
                SubscriptionId = subscriptionId,
                Acceptor = acceptor,
                WaitBeforeRetryingFetch = TimeSpan.FromSeconds(10)
            };
        }

        public void Work()
        {
            SubscriptionProcessState subState = new FetchingState();
            
            while(_state.CancellationToken.IsCancellationRequested == false && _state != null)
                subState = subState.Work(_state);
        }
    }
}