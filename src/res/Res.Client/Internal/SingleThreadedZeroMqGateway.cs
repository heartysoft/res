﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using NetMQ;
using Res.Client.Exceptions;
using Res.Client.Internal.Logging;
using Res.Client.Internal.NetMQ;
using Res.Protocol;

namespace Res.Client.Internal
{
    public class SingleThreadedZeroMqGateway : ResGateway
    {
        private readonly string _endpoint;
        private readonly TimeSpan _reaperInterval;
        private readonly NetMQContext _ctx;
        private NetMQSocket _socket;
        readonly ConcurrentDictionary<Guid, InflightEntry> _callbacks = new ConcurrentDictionary<Guid, InflightEntry>();
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        private DateTime _reapTime;
        private volatile bool _running = true;

        public SingleThreadedZeroMqGateway(string endpoint, TimeSpan reaperInterval)
        {
            Log.DebugFormat("[STZMG] Starting. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
            _ctx = NetMQContext.Create();
            _endpoint = endpoint;
            _reaperInterval = reaperInterval;
            _socket = connect();
            _reapTime = DateTime.Now.Add(reaperInterval);
        }

        public bool ProcessResponse()
        {
            bool processed = false;
            while (_socket.Poll(TimeSpan.FromSeconds(0)) && _running)
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
                _reapTime = DateTime.Now.Add(_reaperInterval);

                var toRemove = new List<Guid>();

                foreach (var callback in _callbacks.Values)
                    if (callback.ShouldDrop())
                    {
                        Log.Debug("[STZMG] Reaping dead request.");
                        callback.Drop();
                        toRemove.Add(callback.RequestId);
                    }

                foreach (var requestId in toRemove)
                {
                    InflightEntry entry;
                    _callbacks.TryRemove(requestId, out entry);
                }
            }
        }

        void socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            try
            {
                var msg = e.Socket.ReceiveMultipartMessage();
                Log.DebugFormat("[STZMG] Received a message. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);

                //address frames
                var count = msg.FrameCount;
                for (int i = 0; i < count; i++)
                {
                    var f = msg.Pop();
                    if (f.BufferSize == 0)
                        break;
                }

                if (msg.FrameCount == 0)
                {
                    Log.Warn("[STZMG] Received a malformed message with non-empty frames (signalling end of routing frames.");
                    return;
                }

                var protocol = msg.Pop().ConvertToString();

                if (protocol != ResProtocol.ResClient01)
                    throw new UnsupportedProtocolException(protocol);

                var requestId = msg.PopGuid();

                InflightEntry callback;
                if (_callbacks.TryRemove(requestId, out callback))
                {
                    callback.ProcessResult(msg);
                }
                else
                {
                    Log.Debug(string.Format("[STZMG] Request Id {0} callback not found. This could be due to receiving a response after timeout has passed.", requestId));
                }
            }
            catch (Exception ex)
            {
                Log.WarnException("[STZMG] Message dropped. Possibly due to protocol violation.", ex);
            }
        }

        public void SendRequest(PendingResRequest pendingRequest)
        {
            var requestId = Guid.NewGuid();

            try
            {
                var callback = pendingRequest.Send(_socket, requestId);
                _callbacks[requestId] = new InflightEntry(requestId, pendingRequest, callback);
            }
            catch (NetMQException)
            {
                _socket = connect();
            }

        }

        private NetMQSocket connect()
        {
            if (_socket != null)
            {
                Log.DebugFormat("[STZMG] Disposing old socket. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
                _socket.ReceiveReady -= socket_ReceiveReady;
                _socket.Options.Linger = TimeSpan.FromSeconds(0);
                _socket.Dispose();
                _socket = null;
            }

            var spinner = new SpinWait();

            while (_running)
            {
                Log.DebugFormat("[STZMG] Creating new socket. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
                var socket = _ctx.CreateDealerSocket();
                socket.ReceiveReady += socket_ReceiveReady;
                
                try
                {
                    socket.Connect(_endpoint);
                    return socket;
                }
                catch (NetMQException e)
                {
                    Log.WarnFormat("[STZMG] Error connecting to socket. Retrying... Thread ID: {0}", e, Thread.CurrentThread.ManagedThreadId);
                    socket.Options.Linger = TimeSpan.FromSeconds(0);
                    socket.Dispose();
                    spinner.SpinOnce();
                }
            }

            return null; //terminating.
        }


        private class InflightEntry
        {
            public Guid RequestId { get; private set; }
            private readonly PendingResRequest _request;
            private readonly Action<NetMQMessage> _resultProcessor;

            public InflightEntry(Guid requestId, PendingResRequest request, Action<NetMQMessage> resultProcessor)
            {
                RequestId = requestId;
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

        private bool _disposed = false;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _running = false;
            shutdown();
        }

        private void shutdown()
        {
            Log.InfoFormat("[STZMG] Shutting down. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
            if (_socket != null)
            {
                _socket.ReceiveReady -= socket_ReceiveReady;
                _socket.Options.Linger = TimeSpan.FromSeconds(0);
                _socket.Dispose();
                Log.DebugFormat("[STZMG] Socket disposed. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
                _ctx.Dispose();
                Log.DebugFormat("[STZMG] Context disposed. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
            }
        }
    }
}