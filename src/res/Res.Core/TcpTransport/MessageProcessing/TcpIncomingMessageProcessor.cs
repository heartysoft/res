using System;
using System.Globalization;
using System.Linq;
using System.Text;
using NetMQ;
using Res.Core.TcpTransport.Exceptions;
using Res.Protocol;

namespace Res.Core.TcpTransport.MessageProcessing
{
    public struct TcpIncomingMessageProcessor : MessageProcessor
    {
        private readonly TcpMessageDispatcher _dispatcher;
        private static readonly ErrorResolver ErrorResolver = new ErrorResolver();

        public TcpIncomingMessageProcessor(TcpMessageDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void ProcessMessage(NetMQMessage message, NetMQSocket socket)
        {
            if (message.FrameCount < 3)
                throw new MalformedMessageReceivedException(message.FrameCount);

            var sender = message.PopUntilEmptyFrame();
            var protocolFrame = message.Pop();
            var protocol = protocolFrame.ConvertToString();
            ensureProtocol(protocol);
            var command = message.Pop().ConvertToString();

            _dispatcher.Dispatch(command, sender, message);
        }

        private static void ensureProtocol(string protocol)
        {
            var supportedProtocols = new[] { ResProtocol.ResClient01 };
            if (supportedProtocols.Contains(protocol) == false)
                throw new UnsupportedProtocolException(protocol, supportedProtocols);
        }
    }
}