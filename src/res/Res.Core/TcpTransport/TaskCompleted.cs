using NetMQ;

namespace Res.Core.TcpTransport
{
    public interface TaskCompleted
    {
        void Send(NetMQSocket socket);
    }
}