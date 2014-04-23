using System;

namespace Res.Core.Storage
{
    public class SubscribeRequest
    {
        public int RequestId { get; private set; }
        public string SubscriberId { get; private set; }
        public string Context { get; private set; }
        public string Filter { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime CurrentTime { get; private set; }

        public SubscribeRequest(int requestId, string subscriberId, string context, string filter, DateTime startTime, DateTime currentTime)
        {
            RequestId = requestId;
            SubscriberId = subscriberId;
            Context = context;
            Filter = filter;
            StartTime = startTime;
            CurrentTime = currentTime;
        }
    }
}