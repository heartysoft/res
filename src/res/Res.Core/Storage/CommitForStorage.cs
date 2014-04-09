using System;
using System.Linq;
using Res.Core.StorageBuffering;

namespace Res.Core.Storage
{
    public class CommitForStorage
    {
        public Guid CommitId { get; private set; }
        public EventForStorage[] Events { get; private set; }
        public string Context { get; private set; }
        public string Stream { get; private set; }

        public CommitForStorage(Guid commitId, string context, string stream, params EventForStorage[] events)
        {
            CommitId = commitId;
            Events = events;
            Context = context;
            Stream = stream;
        }

        public CommitForStorage(string context, string stream, params EventForStorage[] events)
            : this(Guid.NewGuid(), context, stream, events)
        {
        }
    }
}