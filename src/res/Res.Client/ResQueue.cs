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
        private long? _allocationId;
        public ResQueue(string context, string queueId, string subscriberId, string filter, DateTime startTime, QueueRequestAcceptor acceptor)
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

        private async Task<QueuedEvents> subscribe(int allocationSize, TimeSpan allocationTimeout, TimeSpan timeout)
        {
            var subscribeToQueueRequest = new SubscribeToQueueRequest(_context, _queueId, _subscriberId, _filter, _startTime, allocationSize, (int)allocationTimeout.TotalMilliseconds);
            var events = await _acceptor.Subscribe(subscribeToQueueRequest, timeout);
            _allocationId = events.AllocationId;
            _initial = false;

            return new QueuedEvents(events.QueueId, events.SubscriberId, events.Events, events.AllocationId, events.TimeOfResponse, this);
        }

        private async Task<QueuedEvents> acknowledgeAndProgress(int expectedMaxCount, TimeSpan allocationTimeout, TimeSpan timeout)
        {
            var request = new AcknowledgeQueueAndFetchNextRequest(_context, _queueId, _subscriberId, 
                _allocationId, expectedMaxCount, (int)allocationTimeout.TotalMilliseconds);
            var events = await _acceptor.AcknowledgeAndFetchNext(request, timeout);

            _allocationId = events.AllocationId;

            return new QueuedEvents(events.QueueId, events.SubscriberId, events.Events, events.AllocationId, events.TimeOfResponse, this);
        }
    }
}