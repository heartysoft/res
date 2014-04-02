using System;

namespace Res.Core.Tests.Storage
{
    public class CommitModel
    {
        public Guid CommitId { get; private set; }
        public string Context { get; private set; }
        public string Stream { get; private set; }
        public EventStorageInfo[] Events { get; set; } 

        public CommitModel(Guid commitId, string context, string stream,EventStorageInfo[] events)
        {
            CommitId = commitId;
            Context = context;
            Stream = stream;
            Events = events;
        }

        protected bool Equals(CommitModel other)
        {
            return CommitId.Equals(other.CommitId) && string.Equals(Context, other.Context) && Equals(Stream, other.Stream);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CommitModel) obj);
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

        public static bool operator ==(CommitModel left, CommitModel right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CommitModel left, CommitModel right)
        {
            return !Equals(left, right);
        }
    }
}