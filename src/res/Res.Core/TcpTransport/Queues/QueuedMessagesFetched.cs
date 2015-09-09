using NetMQ;

namespace Res.Core.TcpTransport.Queues
{
    public class QueuedMessagesFetched : TaskCompleted
    {
        private readonly NetMQMessage _msg;

        public QueuedMessagesFetched(NetMQMessage msg)
        {
            _msg = msg;
        }

        public void Send(NetMQSocket socket)
        {
            socket.SendMultipartMessage(_msg);
        }
    }
}