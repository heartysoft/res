using System;
using System.Threading.Tasks;
using Res.Client.Internal.Subscriptions.Messages;

namespace Res.Client.Internal.Subscriptions
{
    public class SubscriptionRequestAcceptor
    {
        private readonly MultiWriterSingleReaderBuffer _buffer;

        public SubscriptionRequestAcceptor(MultiWriterSingleReaderBuffer buffer)
        {
            _buffer = buffer;
        }

        public Task<SubscribeResponse> SubscribeAsync(string subscriberId, SubscriptionDefinition[] subscriptions, DateTime startTime, TimeSpan timeout)
        {
            var commitRequest = new SubscribeRequest(subscriberId, subscriptions, startTime);
            var task = _buffer.Enqueue<SubscribeResponse>(commitRequest, DateTime.Now.Add(timeout));
            return task;
        }

        public Task<FetchEventsResponse> FetchEventsAsync(FetchEventParameters[] fetchEvents, TimeSpan timeout)
        {
            var fetch = new FetchEventsRequest(fetchEvents);
            var task = _buffer.Enqueue<FetchEventsResponse>(fetch, DateTime.Now.Add(timeout));
            return task;
        }

        public Task<ProgressSubscriptionResponse> ProgressAsync(ProgressSubscriptionEntry[] progress, TimeSpan timeout)
        {
            var request = new ProgressSubscriptionRequest(progress);
            var task = _buffer.Enqueue<ProgressSubscriptionResponse>(request, DateTime.Now.Add(timeout));
            return task;
        }

        public Task<SetSubscriptionTimesResponse> SetAsync(SetSubscriptionEntry[] progress, TimeSpan timeout)
        {
            var request = new SetSubscriptionTimesRequest(progress);
            var task = _buffer.Enqueue<SetSubscriptionTimesResponse>(request, DateTime.Now.Add(timeout));
            return task;
        }
    }
}