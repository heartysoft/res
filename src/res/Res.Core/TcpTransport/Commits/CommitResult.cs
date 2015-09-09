using System.Globalization;
using NetMQ;
using Res.Protocol;

namespace Res.Core.TcpTransport.Commits
{
    public class CommitResult : TaskCompleted
    {
        private readonly string _protocol;
        private readonly CommitContinuationContext _context;
        private readonly ErrorEntry _error;

        public CommitResult(string protocol, CommitContinuationContext context, ErrorEntry error)
        {
            _protocol = protocol;
            _context = context;
            _error = error;
        }
        public void Send(NetMQSocket socket)
        {
            var msg = new NetMQMessage();
            msg.Append(_context.Sender);
            msg.AppendEmptyFrame();
            msg.Append(_protocol);
            msg.Append(_context.RequestId);

            if (_error == null)
            {
                msg.Append(ResCommands.CommitResult);
                msg.Append(_context.CommitId.ToByteArray());
            }
            else
            {
                msg.Append(ResCommands.Error);
                msg.Append(_error.ErrorCode.ToString(CultureInfo.InvariantCulture));
                msg.Append(_error.Message);
            }

            socket.SendMultipartMessage(msg); 
        }
    }
}