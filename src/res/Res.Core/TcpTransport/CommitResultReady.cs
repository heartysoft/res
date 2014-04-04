using System;
using NetMQ;
using Res.Protocol;

namespace Res.Core.TcpTransport
{
    public class CommitResultReady : TaskCompleted
    {
        private readonly string _protocol;
        private readonly CommitContinuationContext _context;
        private readonly string _errorIfPresent;
        

        public CommitResultReady(string protocol, CommitContinuationContext context, string errorIfPresent)
        {
            _protocol = protocol;
            _context = context;
            _errorIfPresent = errorIfPresent;
        }

        public void Send(NetMQSocket socket)
        {
            var msg = new NetMQMessage();
            msg.Append(_protocol);
            msg.Append(ResCommands.CommitResultReady);

            msg.Append(_context.Sender);
            msg.AppendEmptyFrame();
            
            msg.Append(_context.RequestId);
            
            if(string.IsNullOrWhiteSpace(_errorIfPresent))
                msg.AppendEmptyFrame(); //success
            else
                msg.Append(_errorIfPresent);

            msg.Append(_context.CommitId.ToByteArray());
            
            socket.SendMessage(msg); //send back to the receiver, to send back to the client.
        }
    }
}