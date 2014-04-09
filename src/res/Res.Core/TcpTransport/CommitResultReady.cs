using System;
using System.Globalization;
using NetMQ;
using Res.Protocol;

namespace Res.Core.TcpTransport
{
    public class CommitResultReady : TaskCompleted
    {
        private readonly string _protocol;
        private readonly CommitContinuationContext _context;
        private readonly ErrorEntry _error;


        public CommitResultReady(string protocol, CommitContinuationContext context, ErrorEntry error)
        {
            _protocol = protocol;
            _context = context;
            _error = error;
        }

        public void Send(NetMQSocket socket)
        {
            var msg = new NetMQMessage();
            msg.AppendEmptyFrame();
            msg.Append(_protocol);
            msg.Append(ResCommands.CommitResultReady);

            msg.Append(_context.Sender);
            msg.AppendEmptyFrame();
            
            msg.Append(_context.RequestId);

            if (_error == null)
            {
                msg.AppendEmptyFrame(); //success
                msg.AppendEmptyFrame();
            }
            else
            {
                msg.Append(_error.ErrorCode.ToString(CultureInfo.InvariantCulture));
                msg.Append(_error.Message);
            }

            msg.Append(_context.CommitId.ToByteArray());
            
            socket.SendMessage(msg); //send back to the receiver, to send back to the client.
        }
    }
}