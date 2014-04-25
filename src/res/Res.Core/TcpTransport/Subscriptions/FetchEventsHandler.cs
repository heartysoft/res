using System;
using System.Globalization;
using System.Threading;
using NetMQ;
using NetMQ.zmq;
using Res.Core.Storage;
using Res.Core.TcpTransport.MessageProcessing;
using Res.Core.TcpTransport.NetworkIO;
using Res.Protocol;

namespace Res.Core.TcpTransport.Subscriptions
{
    public class FetchEventsHandler : RequestHandler
    {
        private readonly OutBuffer _outBuffer;
        private readonly SubscriptionStorage _storage;
        private SpinWait _spin;

        public FetchEventsHandler(SubscriptionStorage storage, OutBuffer outBuffer)
        {
            _storage = storage;
            _outBuffer = outBuffer;

            _spin = new SpinWait();
        }

        public void Handle(NetMQFrame[] sender, NetMQMessage message)
        {
            // - WHOOHOO...look at the imperative lump o' lard :):)
            // - bite me.
            var requestId = message.Pop();
            var count = int.Parse(message.Pop().ConvertToString());

            for (var i = 0; i < count; i++)
            {
                var subscriptionId = long.Parse(message.Pop().ConvertToString());
                var suggestedCount = int.Parse(message.Pop().ConvertToString());
                var now = DateTime.UtcNow;

                var events = _storage.FetchEvents(subscriptionId, suggestedCount, now);

                var msg = new NetMQMessage();
                msg.Append(sender);
                msg.AppendEmptyFrame();
                msg.Append(ResProtocol.ResClient01);
                msg.Append(requestId);
                msg.Append(ResCommands.EventsFetched);
                msg.Append(subscriptionId.ToString(CultureInfo.InvariantCulture));
                msg.Append(events.Length.ToString(CultureInfo.InvariantCulture));

                foreach (var e in events)
                {
                    msg.Append(e.EventId.ToByteArray());
                    msg.Append(e.Stream);
                    msg.Append(e.Context);
                    msg.Append(e.Sequence.ToString(CultureInfo.InvariantCulture));
                    msg.Append(e.Timestamp.ToBinary().ToString(CultureInfo.InvariantCulture));
                    msg.Append(e.TypeKey ?? string.Empty);
                    msg.Append(e.Headers ?? string.Empty);
                    msg.Append(e.Body ?? string.Empty);
                }

                var eventsFetched = new EventsFetched(msg);

                while(!_outBuffer.Offer(eventsFetched))
                    _spin.SpinOnce();
            }
        }
    }
}