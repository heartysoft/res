using System;
using System.Globalization;
using System.Threading;
using NetMQ;
using Res.Core.Storage;
using Res.Core.TcpTransport.MessageProcessing;
using Res.Core.TcpTransport.NetworkIO;
using Res.Protocol;

namespace Res.Core.TcpTransport.Subscriptions
{
    public class SetSubscriptionTimessHandler : RequestHandler
    {
        private readonly SubscriptionStorage _storage;
        private readonly OutBuffer _outBuffer;
        private SpinWait _spin;

        public SetSubscriptionTimessHandler(SubscriptionStorage subscriptionStorage, OutBuffer outBuffer)
        {
            _storage = subscriptionStorage;
            _outBuffer = outBuffer;
            _spin = new SpinWait();
        }

        public void Handle(NetMQFrame[] sender, NetMQMessage message)
        {
            var requestId = message.Pop();
            var count = int.Parse(message.Pop().ConvertToString());

            var msg = new NetMQMessage();
            msg.Append(sender);
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(requestId);
            msg.Append(ResCommands.SubscriptionsSet);

            msg.Append(count.ToString(CultureInfo.InvariantCulture));

            for (var i = 0; i < count; i++)
            {
                var subscriberIdFrame = message.Pop();
                var subscriberId = subscriberIdFrame.ConvertToString();
                var contextFrame = message.Pop();
                var context = contextFrame.ConvertToString();
                var filterFrame = message.Pop();
                var filter = filterFrame.ConvertToString();
                var setToFrame = message.Pop();
                var setTo = DateTime.FromBinary(long.Parse(setToFrame.ConvertToString()));
                var now = DateTime.UtcNow;

                var request = new SetSubscriptionTimeRequest(i, subscriberId, context, filter, setTo, now);
                _storage.SetSubscriptionTime(request);
                msg.Append("OK");
            }
            
            var progressed = new SubscriptionsSet(msg);
            while (!_outBuffer.Offer(progressed))
                _spin.SpinOnce();
        }
    }
}