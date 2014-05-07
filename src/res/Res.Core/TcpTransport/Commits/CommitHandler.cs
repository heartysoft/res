using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using NetMQ;
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
        SpinWait _spin = new SpinWait();
        
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public CommitHandler(EventStorageWriter writer, OutBuffer outBuffer)
        {
            _writer = writer;
            _outBuffer = outBuffer;
        }

        public void Handle(NetMQFrame[] sender, NetMQMessage message)
        {
            Log.Info("[CommitHandler] Got a commit to write...");
            var requestId = message.Pop();
            var commit = getCommit(message);
            var task = _writer.Store(commit);
            var commitContinuationContext = new CommitContinuationContext(sender, commit.CommitId, requestId);
            task.ContinueWith(onComplete, commitContinuationContext, TaskContinuationOptions.ExecuteSynchronously);
            Log.Info("[CommitHandler] Commit queued up...");
        }

        private void onComplete(Task commitTask, object state)
        {
            Log.Info("[CommitHandler] Write completed, sending back response.");
            var c = (CommitContinuationContext)state;
            Log.Info("[CommitHandler] Got continuation context.");
            var ready = new CommitResultReady2(Protocol, c, _resolver.GetError(commitTask.Exception));
            Log.Info("[CommitHandler] Here you go, out buffer.");
            _outBuffer.OfferAndWaitUntilAccepted(ready);
            Log.Info("[CommitHandler] Commit result queued up in out buffer.");
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
                var timestamp = DateTime.FromBinary(long.Parse(message.Pop().ConvertToString()));
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