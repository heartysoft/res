using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Res.Client.Internal;
using Res.Client.Internal.Subscriptions;
using Res.Client.Internal.Subscriptions.Messages;

namespace Res.Client
{
    public class Subscription
    {
        private readonly string _subscriberId;
        private readonly SubscriptionDefinition[] _subscriptions;
        private readonly SubscriptionRequestAcceptor _acceptor;
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public Subscription(string subscriberId, SubscriptionDefinition[] subscriptions, SubscriptionRequestAcceptor acceptor)
        {
            _subscriberId = subscriberId;
            _subscriptions = subscriptions;
            _acceptor = acceptor;
        }

        public Task Start(Action<SubscribedEvents> handler, DateTime startTime, TimeSpan timeout, TimeSpan retryDelay, CancellationToken token)
        {
            return Task.Factory.StartNew(() => run(handler, startTime, timeout, retryDelay, token), token, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public Task SetSubscriptionTime(DateTime setTo, TimeSpan timeout)
        {
            var setSubs = _subscriptions.Select(x => new SetSubscriptionEntry(_subscriberId, x.Context, x.Filter, setTo)).ToArray();
            return _acceptor.SetAsync(setSubs, timeout);
        }

        private void run(Action<SubscribedEvents> handler, DateTime startTime, TimeSpan timeout, TimeSpan retryDelay, CancellationToken token)
        {
            while (token.IsCancellationRequested == false)
            {
                try
                {
                    var result = _acceptor.SubscribeAsync(_subscriberId, _subscriptions, startTime, timeout).GetAwaiter().GetResult();
                    foreach (var subscriptionId in result.SubscriptionIds)
                    {
                        long id = subscriptionId;
                        Task.Factory.StartNew(() => startProcess(id, handler, _acceptor, timeout, retryDelay, token), token,
                            TaskCreationOptions.LongRunning, TaskScheduler.Default);
                    }

                    return;
                }
                catch (RequestTimedOutPendingSendException)
                {
                    Log.InfoFormat("[Subscription - {0}] Timeout initialising subscription. Retrying in {1} seconds.", _subscriberId, 5);
                    Task.Delay(5000, token).Wait(token);
                }
            }
        }

        private void startProcess(long subscriptionId, Action<SubscribedEvents> handler, SubscriptionRequestAcceptor acceptor, TimeSpan timeout, TimeSpan retryDelay, CancellationToken token)
        {
            var subscription = new SubscriptionProcess(subscriptionId, handler, acceptor, timeout, retryDelay, token);
            subscription.Work();
        }
    }
}