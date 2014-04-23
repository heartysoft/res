using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Markup;
using NetMQ;
using Res.Core.Storage;
using Res.Core.TcpTransport.NetworkIO;
using Res.Protocol;

namespace Res.Core.TcpTransport.Subscriptions
{
    public class SubscribeHandler : MessageProcessing.RequestHandler
    {
        private readonly OutBuffer _outBuffer;
        private readonly SubscriptionStorage _storage;
        private SpinWait _spin;

        public SubscribeHandler(SubscriptionStorage storage)
        {
            _storage = storage;
            _spin = new SpinWait();

        }

        public SubscribeHandler(OutBuffer outBuffer)
        {
            _outBuffer = outBuffer;
        }

        public void Handle(NetMQFrame[] sender, NetMQMessage message)
        {
            var requestId = message.Pop();
            var subscriberId = message.Pop().ConvertToString();
            var count = int.Parse(message.Pop().ConvertToString());

            var requests = new SubscribeRequest[count];

            for (var i = 0; i < count; i++)
            {
                var context = message.Pop().ConvertToString();
                var filter = message.Pop().ConvertToString();
                var startTime = DateTime.FromBinary(long.Parse(message.Pop().ConvertToString()));
                var now = DateTime.UtcNow;

                var request = new SubscribeRequest(i, subscriberId, context, filter, startTime, now);
                requests[i] = request;
            }

            var responses = _storage.Subscribe(requests);

            var msg = new NetMQMessage();

            msg.Append(sender);
            msg.AppendEmptyFrame();
            msg.Append(requestId);
            msg.Append(ResCommands.SubscribeResponse);
            msg.Append(count.ToString(CultureInfo.InvariantCulture));

            for (int i = 0; i < count; i++)
            {
                msg.Append(responses[i].SubscriptionId.ToString(CultureInfo.InvariantCulture));
            }

            var completed = new SubscribeCompleted(msg);
            while(!_outBuffer.Offer(completed))
                _spin.SpinOnce();
            
        }
        
    }
}