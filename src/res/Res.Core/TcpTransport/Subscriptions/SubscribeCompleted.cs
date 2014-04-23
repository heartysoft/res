using NetMQ;

namespace Res.Core.TcpTransport.Subscriptions
{
    public class SubscribeCompleted : TaskCompleted
    {
        private readonly NetMQMessage _msg;

        public SubscribeCompleted(NetMQMessage msg)
        {
            _msg = msg;
        }

        public void Send(NetMQSocket socket)
        {
            socket.SendMessage(_msg);
        }
    }
}