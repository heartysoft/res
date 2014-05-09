using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Res.Client.Internal;
using Res.Client.Internal.Subscriptions;

namespace Res.Client
{
    public class Subscription
    {
        private readonly string _subscriberId;
        private readonly SubscriptionDefinition[] _subscriptions;
        private readonly SubscriptionRequestAcceptor _acceptor;
        private readonly TimeSpan _timeout;
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public Subscription(string subscriberId, SubscriptionDefinition[] subscriptions, SubscriptionRequestAcceptor acceptor)
        {
            _subscriberId = subscriberId;
            _subscriptions = subscriptions;
            _acceptor = acceptor;
            _timeout = TimeSpan.FromSeconds(10);
        }

        public Task Start(Action<SubscribedEvents> handler, CancellationToken token)
        {
            return Task.Factory.StartNew(() => run(handler, token), token, TaskCreationOptions.None, TaskScheduler.Default);
        }

        private void run(Action<SubscribedEvents> handler, CancellationToken token)
        {
            while (token.IsCancellationRequested == false)
            {
                try
                {
                    var result = _acceptor.SubscribeAsync(_subscriberId, _subscriptions, _timeout).GetAwaiter().GetResult();
                    foreach (var subscriptionId in result.SubscriptionIds)
                    {
                        long id = subscriptionId;
                        Task.Factory.StartNew(() => startProcess(id, handler, _acceptor, _timeout, token), token,
                            TaskCreationOptions.LongRunning, TaskScheduler.Default);
                    }

                    return;
                }
                catch (RequestTimedOutPendingSendException)
                {
                    Log.InfoFormat("[Subscription - {0}] Timeout initialising subscription. Retring in {1} seconds.", _subscriberId, 5);
                    Task.Delay(5000, token).Wait(token);
                }
            }
        }

        private void startProcess(long subscriptionId, Action<SubscribedEvents> handler, SubscriptionRequestAcceptor acceptor, TimeSpan timeout, CancellationToken token)
        {
            var subscription = new SubscriptionProcess(subscriptionId, handler, acceptor, timeout, token);
            subscription.Work();
        }
    }
}