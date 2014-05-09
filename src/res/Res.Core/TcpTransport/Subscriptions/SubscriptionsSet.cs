using NetMQ;

namespace Res.Core.TcpTransport.Subscriptions
{
    public class SubscriptionsSet : TaskCompleted
    {
        private readonly NetMQMessage _msg;

        public SubscriptionsSet(NetMQMessage msg)
        {
            _msg = msg;
        }

        public void Send(NetMQSocket socket)
        {
            socket.SendMessage(_msg);
        }
    }
}