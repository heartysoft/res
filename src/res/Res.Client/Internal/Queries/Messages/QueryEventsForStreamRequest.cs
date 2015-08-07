using System;
using System.Collections.Generic;
using System.Globalization;
using NetMQ;
using Res.Client.Exceptions;
using Res.Protocol;

namespace Res.Client.Internal.Queries.Messages
{
    public class QueryEventsForStreamRequest : ResRequest
    {
        private readonly string _context;
        private readonly string _stream;
        private readonly long _fromVersion;
        private readonly long? _maxVersion;

        public QueryEventsForStreamRequest(string context, string stream, long fromVersion, long? maxVersion = null)
        {
            _context = context;
            _stream = stream;
            _fromVersion = fromVersion;
            _maxVersion = maxVersion;
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, string requestId)
        {
            var pending = (PendingResRequest<QueryEventsForStreamResponse>) pendingRequest;
            var msg = new NetMQMessage();
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(ResCommands.QueryEventsByStream);
            msg.Append(requestId);
            msg.Append(_context);
            msg.Append(_stream);
            msg.Append(BitConverter.GetBytes(_fromVersion));
            if(_maxVersion.HasValue)
                msg.Append(BitConverter.GetBytes(_maxVersion.Value));
            else
                msg.AppendEmptyFrame();

            socket.SendMessage(msg);

            return m =>
            {
                var command = m.Pop().ConvertToString();

                if (command == ResCommands.Error)
                {
                    var errorCode = m.Pop().ConvertToString();
                    var errorDetails = m.Pop().ConvertToString();
                    ErrorResolver.RaiseException(errorCode, errorDetails, pending.SetException);
                    return;
                }

                if (command != ResCommands.QueryEventsByStreamResponse)
                    pending.SetException(new UnsupportedCommandException(command));


                var count = BitConverter.ToInt32(m.Pop().Buffer, 0);

                var events = new EventInStorage[count];

                for (var i = 0; i < count; i++)
                {
                    var id = new Guid(m.Pop().ToByteArray());
                    var streamId = m.Pop().ConvertToString();
                    var context = m.Pop().ConvertToString();
                    var sequence = BitConverter.ToInt64(m.Pop().Buffer, 0);
                    var timestamp = DateTime.FromBinary(BitConverter.ToInt64(m.Pop().Buffer, 0));
                    var type = m.Pop().ConvertToString();
                    var headers = m.Pop().ConvertToString();
                    var body = m.Pop().ConvertToString();

                    events[i] = new EventInStorage(context, streamId, sequence, type, id, headers, body, timestamp);
                }

                var result = new QueryEventsForStreamResponse(_context, _stream, events);
                pending.SetResult(result);
            };
        }
    }
}