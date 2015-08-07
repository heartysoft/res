using System;
using System.Collections.Generic;
using System.Globalization;
using NetMQ;
using Res.Client.Exceptions;
using Res.Client.Internal.NetMQ;
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

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, Guid requestId)
        {
            var pending = (PendingResRequest<QueryEventsForStreamResponse>) pendingRequest;
            var msg = new NetMQMessage();
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(ResCommands.QueryEventsByStream);
            msg.Append(requestId.ToByteArray());
            msg.Append(_context);
            msg.Append(_stream);
            msg.Append(_fromVersion.ToNetMqFrame());
            msg.Append(_maxVersion.ToNetMqFrame());

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


                var count = m.PopInt32();

                var events = new EventInStorage[count];

                for (var i = 0; i < count; i++)
                {
                    var id = new Guid(m.Pop().ToByteArray());
                    var streamId = m.Pop().ConvertToString();
                    var context = m.Pop().ConvertToString();
                    var sequence = m.PopInt64();
                    var timestamp = m.PopDateTime();;
                    var type = m.PopString();
                    var headers = m.PopStringOrNull();
                    var body = m.PopString();

                    events[i] = new EventInStorage(context, streamId, sequence, type, id, headers, body, timestamp);
                }

                var result = new QueryEventsForStreamResponse(_context, _stream, events);
                pending.SetResult(result);
            };
        }
    }
}