using System;
using System.Collections.Generic;
using System.Globalization;
using Common.Logging;
using NetMQ;
using Res.Protocol;

namespace Res.Core.TcpTransport
{
    public class ErrorHandlingMessageProcessor : MessageProcessor
    {
        private readonly MessageProcessor _processor;
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
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

            try
            {
                _processor.ProcessMessage(message, socket);
            }
            catch (Exception e)
            {
                Log.Info("[EHMessageProcessor] Error processing message.", e);
                var entry = _errorResolver.GetError(e);

                if (entry != null)
                {
                    var msg = new NetMQMessage();

                    foreach (var frame in sender)
                    {
                        msg.Append(frame);    
                    }                    

                    msg.AppendEmptyFrame();

                    msg.Append(ResProtocol.ResClient01);
                    msg.Append(ResCommands.Error);
                    msg.Append(entry.ErrorCode.ToString(CultureInfo.InvariantCulture));
                    msg.Append(entry.Message);
                }
            }
        }
    }
}