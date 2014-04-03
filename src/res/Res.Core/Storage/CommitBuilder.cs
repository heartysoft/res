using System;
using System.Collections.Generic;
using System.Linq;

namespace Res.Core.Storage
{
    public class CommitBuilder
    {
        readonly Dictionary<CommitInfo, List<EventForStorage>> _entries = new Dictionary<CommitInfo, List<EventForStorage>>(); 

        public ExpectingEvent NewCommit(Guid commitId, string context, string stream)
        {
            return new ExpectingEvent(new CommitInfo(commitId, context, stream), this);        
        }

        public class ExpectingEvent
        {
            private readonly CommitInfo _commit;
            private readonly CommitBuilder _instance;

            public ExpectingEvent(CommitInfo commit, CommitBuilder instance)
            {
                _commit = commit;
                _instance = instance;
            }

            public ExpectingAnotherEventOrCommit Event(EventForStorage e)
            {
                _instance._entries[_commit] = new List<EventForStorage> {e};
                return new ExpectingAnotherEventOrCommit(_commit, _instance);
            }
        }

        public class ExpectingAnotherEventOrCommit
        {
            private readonly CommitInfo _commit;
            private readonly CommitBuilder _instance;

            public ExpectingAnotherEventOrCommit(CommitInfo commit, CommitBuilder instance)
            {
                _commit = commit;
                _instance = instance;
            }

            public ExpectingAnotherEventOrCommit Event(EventForStorage e)
            {
                _instance._entries[_commit].Add(e);
                return this;
            }

            public ExpectingEvent NewCommit(Guid commitId, string context, string stream)
            {
                return new ExpectingEvent(new CommitInfo(commitId, context, stream), _instance);
            }

            public CommitsForStorage Build()
            {
                return _instance.Build();
            }
        }

        private CommitsForStorage Build()
        {
            var commits =
                _entries.Keys.Select(x => new CommitForStorage(x.CommitId, x.Context, x.Stream, _entries[x].ToArray())).ToArray();
            return new CommitsForStorage(commits);
        }

        public class CommitInfo
        {
            public Guid CommitId { get; private set; }
            public string Context { get; private set; }
            public string Stream { get; private set; }

            public CommitInfo(Guid commitId, string context, string stream)
            {
                CommitId = commitId;
                Context = context;
                Stream = stream;
            }

            protected bool Equals(CommitInfo other)
            {
                return CommitId.Equals(other.CommitId) && string.Equals(Context, other.Context) && Equals(Stream, other.Stream);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((CommitInfo) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = CommitId.GetHashCode();
                    hashCode = (hashCode*397) ^ (Context != null ? Context.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (Stream != null ? Stream.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static bool operator ==(CommitInfo left, CommitInfo right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(CommitInfo left, CommitInfo right)
            {
                return !Equals(left, right);
            }
        }
    }
}