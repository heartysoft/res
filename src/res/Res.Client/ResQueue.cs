using System;
using System.Threading.Tasks;
using Res.Client.Internal.Queues;
using Res.Client.Internal.Queues.Messages;

namespace Res.Client
{
    public class ResQueue
    {
        private readonly string _queueId;
        private readonly string _subscriberId;
        private readonly string _context;
        private readonly string _filter;
        private readonly DateTime _startTime;
        private readonly QueueRequestAcceptor _acceptor;

        private bool _initial = true;
        private long _startMarker;
        private long _endMarker;
        public ResQueue(string queueId, string subscriberId, string context, string filter, DateTime startTime, QueueRequestAcceptor acceptor)
        {
            _queueId = queueId;
            _subscriberId = subscriberId;
            _context = context;
            _filter = filter;
            _startTime = startTime;
            _acceptor = acceptor;
        }

        public Task<QueuedEvents> Next(int allocationSize, TimeSpan allocationTimeout, TimeSpan timeout)
        {
            if (_initial)
                return subscribe(allocationSize, allocationTimeout, timeout);
            
            return acknowledgeAndProgress(allocationSize, allocationTimeout, timeout);
        }

        public Task<QueuedEvents> RefetchWithoutAcknowledging(int allocationSize, TimeSpan allocationTimeout,
            TimeSpan timeout)
        {
            return subscribe(allocationSize, allocationTimeout, timeout);
        }

        private async Task<QueuedEvents> acknowledgeAndProgress(int expectedMaxCount, TimeSpan allocationTimeout, TimeSpan timeout)
        {
            var request = new AcknowledgeQueueAndFetchNextRequest(_queueId, _subscriberId, _startMarker, _endMarker, expectedMaxCount, (int)allocationTimeout.TotalMilliseconds);
            var events = await _acceptor.AcknowledgeAndFetchNext(request, timeout);

            _startMarker = events.StartMarker;
            _endMarker = events.EndMarker;
            _initial = false;

            return new QueuedEvents(events.QueueId, events.SubscriberId, events.Events, events.StartMarker, events.EndMarker, events.TimeOfResponse, this);
        }

        private async Task<QueuedEvents> subscribe(int allocationSize, TimeSpan allocationTimeout, TimeSpan timeout)
        {
            var subscribeToQueueRequest = new SubscribeToQueueRequest(_queueId, _subscriberId, _context, _filter, _startTime, allocationSize, (int)allocationTimeout.TotalMilliseconds);
            var events = await _acceptor.Subscribe(subscribeToQueueRequest, timeout);
            _startMarker = events.StartMarker;
            _endMarker = events.EndMarker;

            return new QueuedEvents(events.QueueId, events.SubscriberId, events.Events, events.StartMarker, events.EndMarker, events.TimeOfResponse, this);
        }
    }
}