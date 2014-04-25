using System;
using System.Threading;
using Common.Logging;

namespace Res.Client.Internal.Subscriptions
{
    public class SubscriptionProcess
    {
        private readonly SubscriptionState _state;
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public SubscriptionProcess(long subscriptionId, Action<SubscribedEvents> handler, SubscriptionRequestAcceptor acceptor, TimeSpan timeout, CancellationToken token)
        {
            _state = new SubscriptionState
            {
                CancellationToken = token,
                DefaultRequestTimeOut = timeout,
                FetchEventsBatchSize = 128,
                Handler = handler,
                SubscriptionId = subscriptionId,
                Acceptor = acceptor,
                WaitBeforeRetryingFetch = TimeSpan.FromSeconds(10)
            };
        }

        public void Work()
        {
            Log.Debug("[SubscriptionProcess] Starting with fetch.");
            SubscriptionProcessState subState = new FetchingState();

            while (_state.CancellationToken.IsCancellationRequested == false && _state != null)
            {
                subState = subState.Work(_state);
                Log.Debug("[SubscriptionProcess] State transition.");
            }
        }
    }
}