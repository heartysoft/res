using System;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NLog;
using Res.Core.Storage;
using Res.Core.StorageBuffering;
using Res.Core.TcpTransport.MessageProcessing;
using Res.Core.TcpTransport.NetworkIO;
using Res.Protocol;

namespace Res.Core.TcpTransport.Commits
{
    public class CommitHandler : RequestHandler
    {
        private readonly EventStorageWriter _writer;
        private readonly OutBuffer _outBuffer;
        readonly ErrorResolver _resolver = new ErrorResolver();
        private const string Protocol = ResProtocol.ResClient01; //parsing based on this. Maybe move elsewhere when more protocols are present.
        
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public CommitHandler(EventStorageWriter writer, OutBuffer outBuffer)
        {
            _writer = writer;
            _outBuffer = outBuffer;
        }

        public void Handle(NetMQFrame[] sender, NetMQMessage message)
        {
            Log.Debug("[CommitHandler] Got a commit to write...");
            var requestId = message.Pop();
            var commit = getCommit(message);
            var task = _writer.Store(commit);
            var commitContinuationContext = new CommitContinuationContext(sender, commit.CommitId, requestId);
            task.ContinueWith(onComplete, commitContinuationContext, TaskContinuationOptions.ExecuteSynchronously);
            Log.Debug("[CommitHandler] Commit queued up...");
        }

        private void onComplete(Task commitTask, object state)
        {
            Log.Debug("[CommitHandler] Write completed, sending back response.");
            var c = (CommitContinuationContext)state;
            Log.Debug("[CommitHandler] Got continuation context.");
            var ready = new CommitResult(Protocol, c, _resolver.GetError(commitTask.Exception));
            Log.Debug("[CommitHandler] Here you go, out buffer.");
            _outBuffer.OfferAndWaitUntilAccepted(ready);
            Log.Debug("[CommitHandler] Commit result queued up in out buffer.");
        }

        private CommitForStorage getCommit(NetMQMessage message)
        {
            var context = message.Pop().ConvertToString();
            var stream = message.Pop().ConvertToString();
            var expectedVersion = BitConverter.ToInt64(message.Pop().Buffer, 0);
            var eventCount = BitConverter.ToInt32(message.Pop().Buffer, 0);

            var events = new EventForStorage[eventCount];

            for (int i = 0; i < eventCount; i++)
            {
                var eventId = new Guid(message.Pop().ToByteArray());
                var timestamp = DateTime.FromBinary(BitConverter.ToInt64(message.Pop().Buffer, 0));
                var typeKey = message.Pop().ConvertToString();
                var headers = message.Pop().ConvertToString();
                var body = message.Pop().ConvertToString();

                //-1 to override concurrency check. Being lazy and not using a constant.
                var version = expectedVersion == -1 ? -1 : expectedVersion + i;

                events[i] = new EventForStorage(eventId, version, timestamp, typeKey, body, headers);
            }

            return new CommitForStorage(context, stream, events);
        }
    }
}