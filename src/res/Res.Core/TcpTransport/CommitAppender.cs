using System;
using System.Threading.Tasks;
using NetMQ;
using Res.Core.Storage;
using Res.Core.StorageBuffering;
using Res.Protocol;

namespace Res.Core.TcpTransport
{

    public class ErrorEntry
    {
        public string ErrorCode { get; private set; }
        public string Message { get; private set; }

        public ErrorEntry(string errorCode, string message)
        {
            ErrorCode = errorCode;
            Message = message;
        }
    }

    public class ErrorResolver
    {
        public ErrorEntry GetError(Exception e)
        {
            if (e == null)
                return null;


            throw new NotImplementedException();
        }
    }

    public class CommitAppender
    {
        private readonly EventStorageWriter _writer;
        private readonly Sink _sink;
        private const string Protocol = ResProtocol.ResClient01; //parsing based on this. Maybe move elsewhere when more protocols are present.
        readonly ErrorResolver _resolver = new ErrorResolver();
        public CommitAppender(EventStorageWriter writer, Sink sink)
        {
            _writer = writer;
            _sink = sink;
        }

        /// <summary>
        /// Appends a commit. Important: The message has the return address, protocol and command frames stripped off before getting here.
        /// </summary>
        /// <param name="sender">The requester address frame.</param>
        /// <param name="message">The message with the return address, protocol and command stripped off.</param>
        public void Append(NetMQFrame[] sender, NetMQMessage message)
        {
            var requestId = message.Pop();
            var commit = getCommit(message);
            var task = _writer.Store(commit);
            var commitContinuationContext = new CommitContinuationContext(sender, commit.CommitId, requestId);
            task.ContinueWith(onComplete, commitContinuationContext, TaskContinuationOptions.ExecuteSynchronously);
        }

        private void onComplete(Task commitTask, object state)
        {
            var c = (CommitContinuationContext)state;

            var ready = new CommitResultReady(Protocol, c, _resolver.GetError(commitTask.Exception));
            _sink.EnqueResult(ready);    
        }

        private CommitForStorage getCommit(NetMQMessage message)
        {
            var context = message.Pop().ConvertToString();
            var stream = message.Pop().ConvertToString();
            var expectedVersion = int.Parse(message.Pop().ConvertToString());
            var eventCount = int.Parse(message.Pop().ConvertToString());

            var events = new EventForStorage[eventCount];

            for (int i = 0; i < eventCount; i++)
            {
                var eventId = new Guid(message.Pop().ToByteArray());
                var timestamp = new DateTime(long.Parse(message.Pop().ConvertToString()));
                var typeKey = message.Pop().ConvertToString();
                var headers = message.Pop().ConvertToString();
                var body = message.Pop().ConvertToString();

                events[i] = new EventForStorage(eventId, expectedVersion + i, timestamp, typeKey, body, headers);
            }

            return new CommitForStorage(context, stream, events);
        }
    }
}