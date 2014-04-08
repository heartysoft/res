using System;
using System.Globalization;
using NetMQ;
using Res.Protocol;

namespace Res.Client.Internal
{
    public class Committer
    {
        public static Action<NetMQMessage> Send(NetMQSocket socket, PendingResRequest<CommitResponse> request, string requestId)
        {
            var req = (CommitRequest)request.Request;

            var msg = new NetMQMessage();
            msg.AppendEmptyFrame();
            msg.Append(ResProtocol.ResClient01);
            msg.Append(ResCommands.AppendCommit);
            msg.Append(requestId);
            msg.Append(req.Context);
            msg.Append(req.Stream);
            msg.Append(req.ExpectedVersion.ToString(CultureInfo.InvariantCulture));
            msg.Append(req.Events.Length.ToString(CultureInfo.InvariantCulture));

            foreach (var e in req.Events)
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
                var command = msg.Pop().ConvertToString();

                if(command != ResCommands.CommitResult)
                    throw new UnsupportedCommandException(command);

                var errorCode = msg.Pop().ConvertToString();
                var errorDetails = msg.Pop().ConvertToString();
                var commitId = new Guid(msg.Pop().ToByteArray());

                ErrorResolver.RaiseExceptionIfNeeded(errorCode, errorDetails, request.SetException);

                var result = new CommitResponse(commitId);
                request.SetResult(result);
            };
        }
    }
}