using NetMQ;

namespace Res.Core.TcpTransport.MessageProcessing
{
    public interface MessageProcessor
    {
        void ProcessMessage(NetMQMessage message, NetMQSocket socket);
    }
}