using NetMQ;

namespace Res.Core.TcpTransport.Subscriptions
{
    public class EventsFetched : TaskCompleted
    {
        private readonly NetMQMessage _msg;

        public EventsFetched(NetMQMessage msg)
        {
            _msg = msg;
        }

        public void Send(NetMQSocket socket)
        {
            socket.SendMessage(_msg);
        }
    }
}