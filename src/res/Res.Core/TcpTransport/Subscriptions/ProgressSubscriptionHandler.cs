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
    public class ProgressSubscriptionHandler : RequestHandler
    {
        private readonly OutBuffer _outBuffer;
        private readonly SubscriptionStorage _storage;
        private SpinWait _spin;

        public ProgressSubscriptionHandler(SubscriptionStorage storage, OutBuffer outBuffer)
        {
            _storage = storage;
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
            msg.Append(ResCommands.SubscriptionsProgressed);
            
            msg.Append(count.ToString(CultureInfo.InvariantCulture));
            
            for (var i = 0; i < count; i++)
            {
                var subscriptionId = long.Parse(message.Pop().ConvertToString());
                var lastEventTime = DateTime.FromBinary(long.Parse(message.Pop().ConvertToString()));
                var now = DateTime.UtcNow;

                _storage.ProgressSubscription(subscriptionId, lastEventTime, now);
                msg.Append(subscriptionId.ToString(CultureInfo.InvariantCulture));
            }


            var progressed = new SubscriptionsProgressed(msg);
            while(!_outBuffer.Offer(progressed))
                _spin.SpinOnce();
        }
    }
}