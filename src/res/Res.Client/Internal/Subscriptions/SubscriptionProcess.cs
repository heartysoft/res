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
                WaitBeforeRetryingFetch = TimeSpan.FromSeconds(0.5)
            };
        }

        public void Work()
        {
            Log.Debug("[SubscriptionProcess] Starting with fetch.");
            SubscriptionProcessState subState = new FetchingState();

            while (_state.CancellationToken.IsCancellationRequested == false && _state != null)
            {
                try
                {
                    subState = subState.Work(_state);
                    Log.DebugFormat("[SubscriptionProcess] State transition to {0}", subState.GetType().Name);
                }
                catch (OperationCanceledException)
                {
                    Log.Debug("[SubscriptionProcess] Operation cancelled. Exiting mainloop.");
                    return;
                }
                catch (Exception e)
                {
                    Log.ErrorFormat("[SubscriptionProcess] Global error in subscription mainloop.", e);
                }
            }
        }
    }
}