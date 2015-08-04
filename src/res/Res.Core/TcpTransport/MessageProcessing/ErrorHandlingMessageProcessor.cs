using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using NetMQ;
using NLog;
using Res.Protocol;

namespace Res.Core.TcpTransport.MessageProcessing
{
    public class ErrorHandlingMessageProcessor : MessageProcessor
    {
        private readonly MessageProcessor _processor;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly ErrorResolver _errorResolver;

        public ErrorHandlingMessageProcessor(MessageProcessor processor)
        {
            _processor = processor;
            _errorResolver = new ErrorResolver();
        }

        public void ProcessMessage(NetMQMessage message, NetMQSocket socket)
        {
            var sender = new List<NetMQFrame>(message.FrameCount);

            for (int i = 0; i < message.FrameCount; i++)
            {
                var frame = message[i];
                if (frame.BufferSize == 0)
                    break;

                sender.Add(frame);
            }

            var protocolFrame = message[sender.Count + 1];
            var commandFrame = message[sender.Count + 2];
            var requestId = message[sender.Count + 3];

            try
            {
                _processor.ProcessMessage(message, socket);
            }
            catch (Exception e)
            {
                Log.Warn("[EHMessageProcessor] Error processing message.", e);
                var entry = _errorResolver.GetError(e);

                if (entry != null)
                {
                    var msg = new NetMQMessage();

                    foreach (var frame in sender)
                    {
                        msg.Append(frame);    
                    }                    

                    msg.AppendEmptyFrame();

                    msg.Append(protocolFrame);
                    msg.Append(requestId);
                    msg.Append(ResCommands.Error);
                    msg.Append(entry.ErrorCode.ToString(CultureInfo.InvariantCulture));
                    msg.Append(entry.Message);
                    socket.SendMessage(msg);
                }
            }
        }
    }
}