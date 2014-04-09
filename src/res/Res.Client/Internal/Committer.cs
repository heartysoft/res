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
                var command = m.Pop().ConvertToString();

                if (command == ResCommands.Error)
                {
                    var errorCode = m.Pop().ConvertToString();
                    var errorDetails = m.Pop().ConvertToString();
                    ErrorResolver.RaiseException(errorCode, errorDetails, request.SetException);
                }

                if(command != ResCommands.CommitResult)
                    request.SetException(new UnsupportedCommandException(command));

                
                var commitId = new Guid(m.Pop().ToByteArray());
                var result = new CommitResponse(commitId);
                request.SetResult(result);
            };
        }
    }
}