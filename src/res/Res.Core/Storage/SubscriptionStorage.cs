﻿using System;

namespace Res.Core.Storage
{
    public interface SubscriptionStorage
    {
        SubscribeResponse[] Subscribe(SubscribeRequest[] requests);
        EventInStorage[] FetchEvents(long subscriptionId, int suggestedCount, DateTime now);
        void ProgressSubscription(long subscriptionId, DateTime expectedNextBookmark, DateTime now);
    }
}