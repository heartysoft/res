using System;
using System.Threading;
using System.Threading.Tasks;
using Res.Client.Internal;
using Res.Client.Internal.Subscriptions;

namespace Res.Client
{
    public class Subscription
    {
        private readonly SubscriptionDefinition[] _subscriptions;
        private readonly Action<SubscribedEvents> _handler;
        private readonly SubscriptionRequestAcceptor _acceptor;
        private readonly TimeSpan _timeout;

        public Subscription(SubscriptionDefinition[] subscriptions, Action<SubscribedEvents> handler, SubscriptionRequestAcceptor acceptor)
        {
            _subscriptions = subscriptions;
            _handler = handler;
            _acceptor = acceptor;
            _timeout = TimeSpan.FromSeconds(10);
        }

        public Task Start(CancellationToken token)
        {
            return Task.Factory.StartNew(() => run(token), token, TaskCreationOptions.None, TaskScheduler.Default);
        }

        private void run(CancellationToken token)
        {
            while (token.IsCancellationRequested == false)
            {
                try
                {
                    var result = _acceptor.SubscribeAsync(_subscriptions, _timeout).Result;
                    foreach (var subscriptionId in result.SubscriptionIds)
                    {
                        long id = subscriptionId;
                        Task.Factory.StartNew(() => startProcess(id, _acceptor, _timeout, token), token,
                            TaskCreationOptions.LongRunning, TaskScheduler.Default);
                    }
                }
                catch (RequestTimedOutPendingSendException)
                {
                }
            }
        }

        private void startProcess(long subscriptionId, SubscriptionRequestAcceptor acceptor, TimeSpan timeout, CancellationToken token)
        {
            var subscription = new SubscriptionProcess(subscriptionId, _handler, acceptor, timeout, token);
            subscription.Work();
        }
    }
}