using NetMQ;

namespace Res.Core.TcpTransport.Queries
{
    public class QueryEventsForStreamLoaded : TaskCompleted
    {
        private readonly NetMQMessage _msg;

        public QueryEventsForStreamLoaded(NetMQMessage msg)
        {
            _msg = msg;
        }

        public void Send(NetMQSocket socket)
        {
            socket.SendMessage(_msg);
        }
    }
}