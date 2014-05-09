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
    public class SetSubscriptionsHandler : RequestHandler
    {
        private readonly SubscriptionStorage _storage;
        private readonly OutBuffer _outBuffer;
        private SpinWait _spin;

        public SetSubscriptionsHandler(SubscriptionStorage subscriptionStorage, OutBuffer outBuffer)
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
                var subscriptionId = long.Parse(message.Pop().ConvertToString());
                var resetToFrame = message.Pop();
                var resetTo = DateTime.FromBinary(long.Parse(resetToFrame.ConvertToString()));
                var lastEventTime = DateTime.FromBinary(long.Parse(message.Pop().ConvertToString()));
                var now = DateTime.UtcNow;

                _storage.SetSubscription(subscriptionId, resetTo, lastEventTime, now);
                msg.Append(subscriptionId.ToString(CultureInfo.InvariantCulture));
                msg.Append(resetToFrame);
            }
            
            var progressed = new SubscriptionsProgressed(msg);
            while (!_outBuffer.Offer(progressed))
                _spin.SpinOnce();
        }
    }
}