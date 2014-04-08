using System;
using System.Collections.Generic;

namespace Res.Client
{
    public class CommitRequest : ResRequest
    {
        public readonly string Context;
        public readonly string Stream;
        public readonly EventData[] Events;
        public readonly long ExpectedVersion;

        public CommitRequest(string context, string stream, EventData[] events, long expectedVersion)
        {
            ExpectedVersion = expectedVersion;
            Context = context;
            Stream = stream;
            Events = events;
        }
    }
}