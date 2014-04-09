using NetMQ;
using Res.Protocol;

namespace Res.Core.TcpTransport
{
    public class ResultProcessor
    {
        /// <summary>
        /// Returns result to client. Important: The message has the return address, protocol and command frames stripped off before getting here.
        /// </summary>
        public void CommitResult(NetMQMessage msgForClient, NetMQSocket respondTo)
        {
            respondTo.SendMessage(msgForClient);
        }
    }
}