using NetMQ;

namespace Res.Core.TcpTransport.Subscriptions
{
    public class SubscriptionsProgressed : TaskCompleted
    {
        private readonly NetMQMessage _msg;

        public SubscriptionsProgressed(NetMQMessage msg)
        {
            _msg = msg;
        }

        public void Send(NetMQSocket socket)
        {
            socket.SendMessage(_msg);
        }
    }
}