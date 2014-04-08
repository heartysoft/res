using System;
using Res.Client.Internal;

namespace Res.Client
{
    public class CommitResponse : ResResponse
    {
        public Guid CommitId { get; set; }

        public CommitResponse(Guid commitId)
        {
            CommitId = commitId;
        }
    }
}