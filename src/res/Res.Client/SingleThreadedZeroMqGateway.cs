using System;
using System.Collections.Concurrent;
using System.Threading;
using Common.Logging;
using NetMQ;
using Res.Protocol;

namespace Res.Client
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
            _endpoint = endpoint;
            _reaperInterval = reaperInterval;
            _ctx = NetMQContext.Create();
            _socket = connect();
            _reapTime = DateTime.Now.Add(reaperInterval);
        }

        public bool ProcessResponse()
        {
            if (!_socket.Poll(TimeSpan.FromSeconds(0)))
                return false;

            return true;
        }

        public void KillRequestsThatHaveTimedOut()
        {
            if (_reapTime <= DateTime.Now)
            {
                _reapTime = DateTime.Now.Add(_reaperInterval);
                foreach (var callback in _callbacks.Values)
                    if (callback.ShouldDrop())
                        callback.Drop();
            }
        }

        void socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            try
            {
                var msg = e.Socket.ReceiveMessage();

                //address frames
                while (true)
                {
                    var f = msg.Pop();
                    if (f.BufferSize == 0)
                        break;
                }

                var protocol = msg.Pop().ConvertToString();

                if (protocol != ResProtocol.ResClient01)
                    throw new UnsupportedProtocolException(protocol);

                var requestId = msg.Pop().ConvertToString();

                InflightEntry callback;
                if(_callbacks.TryRemove(requestId, out callback))
                {
                    callback.ProcessResult(msg);
                    Log.Warn(string.Format("Request Id {0} callback not found. This could be due to receiving a response after timeout has passed.", requestId));
                }
            }
            catch (Exception ex)
            {
                Log.Warn(string.Format("Message dropped. Possibly due to protocol violation.", ex));
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
                _socket.ReceiveReady -= socket_ReceiveReady;
                _socket.Close();
                _socket.Dispose();
                _socket = null;
            }

            var socket = _ctx.CreateDealerSocket();
            var spinner = new SpinWait();

            while (true)
            {
                try
                {
                    socket.ReceiveReady += socket_ReceiveReady;
                    socket.Connect(_endpoint);
                    return socket;
                }
                catch
                {
                    socket.ReceiveReady -= socket_ReceiveReady;
                    socket.Dispose();
                    spinner.SpinOnce();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            if (_socket != null)
            {
                _socket.Close();
                _socket.Dispose();
            }

            _ctx.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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