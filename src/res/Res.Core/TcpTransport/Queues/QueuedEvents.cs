using Res.Core.Storage;

namespace Res.Core.TcpTransport.Queues
{
    public class QueuedEvents
    {
        public long? AllocationId { get; set; }
        public EventInStorage[] Events { get; set; }

        public QueuedEvents(long? allocationId, EventInStorage[] events)
        {
            AllocationId = allocationId;
            Events = events;
        }
    }
}