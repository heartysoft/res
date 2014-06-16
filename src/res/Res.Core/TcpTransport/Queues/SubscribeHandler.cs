using Common.Logging;
using NetMQ;
using Res.Core.TcpTransport.MessageProcessing;
using Res.Core.TcpTransport.NetworkIO;

namespace Res.Core.TcpTransport.Queues
{
    public interface QueueStorage
    {
    }

    public class SubscribeHandler : RequestHandler
    {
        private readonly QueueStorage _storage;
        private readonly OutBuffer _outBuffer;
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        public SubscribeHandler(QueueStorage storage, OutBuffer outBuffer)
        {
            _storage = storage;
            _outBuffer = outBuffer;
        }

        public void Handle(NetMQFrame[] sender, NetMQMessage message)
        {
            Logger.Debug("[Queue_SubscribeHandler] Received subscribe request.");

            var requestId = message.Pop();
            

            throw new System.NotImplementedException();
        }
    }
}