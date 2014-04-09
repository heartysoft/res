using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using NetMQ;
using NetMQ.zmq;
using Res.Protocol;

namespace Res.Core.TcpTransport
{
    public class CoreMessageProcessor : MessageProcessor
    {
        private readonly CommitAppender _appender;
        private readonly ResultProcessor _resultProcessor;

        public CoreMessageProcessor(CommitAppender appender, ResultProcessor resultProcessor)
        {
            _appender = appender;
            _resultProcessor = resultProcessor;
        }

        public void ProcessMessage(NetMQMessage message, NetMQSocket socket)
        {
            if (message.FrameCount < 3)
                throw new MalformedMessageReceivedException(message.FrameCount);

            var sender = message.PopUntilEmptyFrame();
            var protocol = message.Pop().ConvertToString();
            ensureProtocol(protocol);
            var command = message.Pop().ConvertToString();

            switch (command)
            {
                case ResCommands.AppendCommit:
                    _appender.Append(sender, message);
                    break;
                case ResCommands.CommitResultReady:
                    _resultProcessor.CommitResult(message, socket);
                    break;
                default:
                    throw new UnsupportedCommandException(command, protocol);
            }
        }

        public class MalformedMessageReceivedException : Exception
        {
            public MalformedMessageReceivedException(int frameCount) :
                base(string.Format("Received a message with {0} frames. Minimum expected frame count is {1}.", frameCount, 3))
            {
            }
        }

        public class UnsupportedCommandException : Exception
        {
            public UnsupportedCommandException(string command, string protocol)
                : base(string.Format("Command {0} is not supported under protocol {1}", command, protocol))
            {
                
            }
        }

        private static void ensureProtocol(string protocol)
        {
            var supportedProtocols = new[] {ResProtocol.ResClient01};
            if (supportedProtocols.Contains(protocol) == false)
                throw new UnsupportedProtocolException(protocol, supportedProtocols);
        }

        public class UnsupportedProtocolException : Exception
        {
            public UnsupportedProtocolException(string protocol, params string[] requiredProtocol)
                : base(
                    string.Format("Received request with protocol: {0}. Supported protocols: {1}", protocol,
                        string.Join(", ", requiredProtocol)))
            {
            }
        }
    }
}