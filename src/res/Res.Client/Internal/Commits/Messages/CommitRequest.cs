using System;
using System.Globalization;
using NetMQ;
using Res.Client.Exceptions;
using Res.Protocol;

namespace Res.Client.Internal.Commits.Messages
{
    public class CommitRequest : ResRequest
    {
        public readonly string Context;
        public readonly string Stream;
        public readonly EventData[] Events;
        public readonly long ExpectedVersion;

        public CommitRequest(string context, string stream, EventData[] events, long expectedVersion)
        {
            ExpectedVersion = expectedVersion;
            Context = context;
            Stream = stream;
            Events = events;
        }

        public Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest pendingRequest, string requestId)
        {
            var pending = (PendingResRequest<CommitResponse>) pendingRequest;

            var msg = new NetMQMessage();
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(ResCommands.AppendCommit);
            msg.Append(requestId);
            msg.Append(Context);
            msg.Append(Stream);
            msg.Append(ExpectedVersion.ToString(CultureInfo.InvariantCulture));
            msg.Append(Events.Length.ToString(CultureInfo.InvariantCulture));

            foreach (var e in Events)
            {
                msg.Append(e.EventId.ToByteArray());
                var timestamp = e.Timestamp.ToBinary().ToString(CultureInfo.InvariantCulture);
                msg.Append(timestamp);
                msg.Append(e.TypeTag);
                msg.Append(e.Headers);
                msg.Append(e.Body);
            }

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

                if (command != ResCommands.CommitResult)
                    pending.SetException(new UnsupportedCommandException(command));


                var commitId = new Guid(m.Pop().ToByteArray());
                var result = new CommitResponse(commitId);
                pending.SetResult(result);
            };
        }
    }
}