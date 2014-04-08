using NetMQ;
using Res.Protocol;

namespace Res.Core.TcpTransport
{
    public class ResultProcessor
    {
        /// <summary>
        /// Returns result to client. Important: The message has the return address, protocol and command frames stripped off before getting here.
        /// </summary>
        public void CommitResult(NetMQMessage commitMessageFromSink, NetMQSocket respondTo)
        {
            var sender = commitMessageFromSink.PopUntilEmptyFrame();

            var requestId = commitMessageFromSink.Pop();
            var result = commitMessageFromSink.Pop();
            var commitId = commitMessageFromSink.Pop();

            var msg = new NetMQMessage();
            msg.Append(sender);
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01); //currently only one.
            msg.Append(requestId);
            msg.Append(ResCommands.CommitResult);
            msg.Append(result);
            msg.Append(commitId);

            respondTo.SendMessage(msg);
        }
    }
}