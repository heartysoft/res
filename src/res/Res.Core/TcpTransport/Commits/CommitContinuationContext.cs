using System;
using NetMQ;

namespace Res.Core.TcpTransport.Commits
{
    public class CommitContinuationContext
    {
        public NetMQFrame[] Sender { get; set; }
        public Guid CommitId { get; set; }
        public NetMQFrame RequestId { get; set; }

        public CommitContinuationContext(NetMQFrame[] sender, Guid commitId, NetMQFrame requestId)
        {
            Sender = sender;
            CommitId = commitId;
            RequestId = requestId;
        }
    }
}