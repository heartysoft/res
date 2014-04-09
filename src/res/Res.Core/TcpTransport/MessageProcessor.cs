using NetMQ;

namespace Res.Core.TcpTransport
{
    public interface MessageProcessor
    {
        void ProcessMessage(NetMQMessage message, NetMQSocket socket);
    }
}