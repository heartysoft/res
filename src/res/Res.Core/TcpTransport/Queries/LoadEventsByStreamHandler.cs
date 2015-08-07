using System;
using System.Globalization;
using System.Threading;
using NetMQ;
using NetMQ.zmq;
using NLog;
using Res.Core.Storage;
using Res.Core.StorageBuffering;
using Res.Core.TcpTransport.MessageProcessing;
using Res.Core.TcpTransport.NetworkIO;
using Res.Protocol;

namespace Res.Core.TcpTransport.Queries
{
    public class LoadEventsByStreamHandler : RequestHandler
    {
        private readonly EventStorageReader _storage;
        private readonly OutBuffer _buffer;
        private SpinWait _spin;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public LoadEventsByStreamHandler(EventStorageReader storage, OutBuffer buffer)
        {
            _storage = storage;
            _buffer = buffer;
            _spin = new SpinWait();
        }

        public void Handle(NetMQFrame[] sender, NetMQMessage message)
        {
            Logger.Debug("[Query_LoadEventsByStream] Received a request.");

            var requestId = message.Pop();
            var context = message.Pop().ConvertToString();
            var stream = message.Pop().ConvertToString();

            var fromVersion = BitConverter.ToInt64(message.Pop().Buffer, 0);

            long? maxVersion = null;
            var maxVersionFrame = message.Pop();

            if (maxVersionFrame.BufferSize != 0)
                maxVersion = BitConverter.ToInt64(maxVersionFrame.Buffer, 0);

            var events = _storage.LoadEventsForStream(context, stream, fromVersion, maxVersion);

            var msg = new NetMQMessage();
            msg.Append(sender);
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(requestId);
            msg.Append(ResCommands.QueryEventsByStreamResponse);

            var count = events.Length;

            msg.Append(BitConverter.GetBytes(count));

            foreach (var e in events)
            {
                msg.Append(e.EventId.ToByteArray());
                msg.Append(e.Stream);
                msg.Append(e.Context);
                msg.Append(BitConverter.GetBytes(e.Sequence));
                msg.Append(BitConverter.GetBytes(e.Timestamp.ToBinary()));
                msg.Append(e.TypeKey ?? string.Empty);
                msg.Append(e.Headers ?? string.Empty);
                msg.Append(e.Body ?? string.Empty);
            }

            var result = new QueryEventsForStreamLoaded(msg);
            while (!_buffer.Offer(result))
                _spin.SpinOnce();

        }
    }
}