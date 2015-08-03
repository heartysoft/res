using System;
using System.Collections.Generic;
using NetMQ;

namespace Res.Core.TcpTransport.MessageProcessing
{
    public class TcpMessageDispatcher
    {
        readonly Dictionary<string, RequestHandler> _handlers = new Dictionary<string, RequestHandler>();

        public TcpMessageDispatcher Register(string command, RequestHandler handler)
        {
            _handlers[command] = handler;
            return this;
        }

        public void Dispatch(string command, NetMQFrame[] sender, NetMQMessage message)
        {
            _handlers[command].Handle(sender, message);
        }
    }
}