using System;
using System.Collections.Concurrent;
using System.Threading;
using Common.Logging;
using NetMQ;
using Res.Client.Exceptions;
using Res.Protocol;

namespace Res.Client.Internal
{
    public class SingleThreadedZeroMqGateway : ResGateway
    {
        private readonly string _endpoint;
        private readonly TimeSpan _reaperInterval;
        private readonly NetMQContext _ctx;
        private NetMQSocket _socket;
        readonly ConcurrentDictionary<string, InflightEntry> _callbacks = new ConcurrentDictionary<string, InflightEntry>();
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private DateTime _reapTime;

        public SingleThreadedZeroMqGateway(string endpoint, TimeSpan reaperInterval)
        {
            Log.InfoFormat("[STZMG] Starting. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
            _ctx = NetMQContext.Create();
            _endpoint = endpoint;
            _reaperInterval = reaperInterval;
            _socket = connect();
            _reapTime = DateTime.Now.Add(reaperInterval);
        }

        public bool ProcessResponse()
        {
            bool processed = false;
            while (_socket.Poll(TimeSpan.FromSeconds(0)))
            {
                processed = true;
            }

            KillRequestsThatHaveTimedOut();

            return processed;
        }

        public void KillRequestsThatHaveTimedOut()
        {
            if (_reapTime <= DateTime.Now)
            {
                Log.Info("[STZMG] Reaping dead requests.");
                _reapTime = DateTime.Now.Add(_reaperInterval);
                foreach (var callback in _callbacks.Values)
                    if (callback.ShouldDrop())
                        callback.Drop();
            }
        }

        void socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            Log.InfoFormat("[STZMG] Receiving a message. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
            try
            {
                var msg = e.Socket.ReceiveMessage();

                //address frames
                var count = msg.FrameCount;
                for(int i=0; i<count; i++)
                {
                    var f = msg.Pop();
                    if (f.BufferSize == 0)
                        break;
                }

                if (msg.FrameCount == 0)
                {
                    Log.Warn("[STZMG] Received a malformed message with not empty frames (signalling end of routing frames.");
                    return;
                }

                var protocol = msg.Pop().ConvertToString();

                if (protocol != ResProtocol.ResClient01)
                    throw new UnsupportedProtocolException(protocol);

                var requestId = msg.Pop().ConvertToString();

                InflightEntry callback;
                if(_callbacks.TryRemove(requestId, out callback))
                {
                    callback.ProcessResult(msg);
                }
                else
                {
                    Log.Warn(string.Format("Request Id {0} callback not found. This could be due to receiving a response after timeout has passed.", requestId));
                }
            }
            catch (Exception ex)
            {
                Log.Warn("[STZMG] Message dropped. Possibly due to protocol violation.", ex);
            }
        }

        public void SendRequest(PendingResRequest pendingRequest)
        {
            var request = pendingRequest.Request;
            var requestId = Guid.NewGuid().ToString();

            //TODO: OO this out...not many commands yet...
            if (request is CommitRequest)
            {
                try
                {
                    var callback = Committer.Send(_socket, (PendingResRequest<CommitResponse>) pendingRequest, requestId);
                    _callbacks[requestId] = new InflightEntry(pendingRequest, callback);
                }
                catch(NetMQException)
                {
                    _socket = connect();
                }
            }
            else
            {
                throw new NotImplementedException("Only commits for now");
            }
        }

        private NetMQSocket connect()
        {
            if (_socket != null)
            {
                Log.InfoFormat("[STZMG] Disposing old socket. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
                _socket.ReceiveReady -= socket_ReceiveReady;
                _socket.Options.Linger = TimeSpan.FromSeconds(0);
                _socket.Dispose();
                _socket = null;
            }

            Log.InfoFormat("[STZMG] Creating new socket. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
            var socket = _ctx.CreateDealerSocket();
            socket.ReceiveReady += socket_ReceiveReady;

            var spinner = new SpinWait();

            while (true)
            {
                try
                {
                    socket.Connect(_endpoint);
                    return socket;
                }
                catch(NetMQException e)
                {
                    Log.WarnFormat("[STZMG] Error connecting to socket. Retrying... Thread ID: {0}", e, Thread.CurrentThread.ManagedThreadId);
                    socket.Options.Linger = TimeSpan.FromSeconds(0);
                    socket.Dispose();
                    spinner.SpinOnce();
                }
            }
        }

        public void Shutdown()
        {
            Log.InfoFormat("[STZMG] Shutting down. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
            if (_socket != null)
            {
                _socket.ReceiveReady -= socket_ReceiveReady;
                _socket.Options.Linger = TimeSpan.FromSeconds(0);
                _socket.Dispose();
                Log.InfoFormat("[STZMG] Socket disposed. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
                _ctx.Dispose();
                Log.InfoFormat("[STZMG] Context disposed. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
            }
        }


        private class InflightEntry
        {
            private readonly PendingResRequest _request;
            private readonly Action<NetMQMessage> _resultProcessor;

            public InflightEntry(PendingResRequest request, Action<NetMQMessage> resultProcessor)
            {
                _request = request;
                _resultProcessor = resultProcessor;
            }

            public bool ShouldDrop()
            {
                return _request.ShouldDrop();
            }

            public void Drop()
            {
                _request.Drop();
            }

            public void ProcessResult(NetMQMessage m)
            {
                _resultProcessor(m);
            }
        }
    }
}