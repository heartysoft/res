using NetMQ;
using Res.Core.TcpTransport.NetworkIO;

namespace Res.Core.TcpTransport.Subscriptions
{
    public class SubscribeHandler : MessageProcessing.RequestHandler
    {
        private readonly TcpGateway _gateway;

        public SubscribeHandler(TcpGateway gateway)
        {
            _gateway = gateway;
        }

        public void Handle(NetMQFrame[] sender, NetMQMessage message)
        {
            
        }
    }
}