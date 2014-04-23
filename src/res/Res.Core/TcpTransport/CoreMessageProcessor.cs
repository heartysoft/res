using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using NetMQ;
using NetMQ.zmq;
using Res.Core.TcpTransport.Exceptions;
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
                case ResCommands.ResultReady:
                    _resultProcessor.CommitResult(message, socket);
                    break;
                default:
                    throw new UnsupportedCommandException(command, protocol);
            }
        }

        private static void ensureProtocol(string protocol)
        {
            var supportedProtocols = new[] {ResProtocol.ResClient01};
            if (supportedProtocols.Contains(protocol) == false)
                throw new UnsupportedProtocolException(protocol, supportedProtocols);
        }

        
    }
}