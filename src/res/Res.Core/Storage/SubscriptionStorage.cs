using System;
using System.Collections.Generic;

namespace Res.Core.Storage
{
    public interface SubscriptionStorage
    {
        SubscribeResponse[] Subscribe(IEnumerable<SubscribeRequest> requests);
        EventInStorage[] FetchEvents(long subscriptionId, int suggestedCount, DateTime now);
        void ProgressSubscription(long subscriptionId, DateTime expectedNextBookmark, DateTime now);
        void SetSubscription(long subscriptionId, DateTime resetTo, DateTime expectedNextBookmark, DateTime now);
    }
}