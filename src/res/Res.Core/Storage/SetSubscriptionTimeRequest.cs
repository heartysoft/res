using System;

namespace Res.Core.Storage
{
    public class SetSubscriptionTimeRequest
    {
        public int RequestId { get; private set; }
        public string SubscriberId { get; private set; }
        public string Context { get; private set; }
        public string Filter { get; private set; }
        public DateTime SetTo { get; private set; }
        public DateTime CurrentTime { get; private set; }

        public SetSubscriptionTimeRequest(int requestId, string subscriberId, string context, string filter, DateTime setTo, DateTime currentTime)
        {
            RequestId = requestId;
            SubscriberId = subscriberId;
            Context = context;
            Filter = filter;
            SetTo = setTo;
            CurrentTime = currentTime;
        }
    }
}