using NetMQ;

namespace Res.Core.TcpTransport.MessageProcessing
{
    public interface RequestHandler
    {
        void Handle(NetMQFrame[] sender, NetMQMessage message);
    }
}