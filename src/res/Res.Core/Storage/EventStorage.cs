using System;
using System.Collections.Generic;

namespace Res.Core.Storage
{
    public interface EventStorage
    {
        CommitResults Store(CommitsForStorage commits);
        EventInStorage[] LoadEvents(string context, string streamId, long fromVersion = 0, long? maxVersion = null);
        Dictionary<Guid, EventInStorage> FetchEvent(FetchEventRequest[] request);
        void Verify();
    }
}