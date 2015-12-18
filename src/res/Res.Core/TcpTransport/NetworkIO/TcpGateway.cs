using System;
using System.Threading;
using NetMQ;
using NLog;
using Res.Core.TcpTransport.MessageProcessing;

namespace Res.Core.TcpTransport.NetworkIO
{
    public class TcpGateway
    {
        private readonly NetMQContext _ctx;
        private readonly string _endpoint;
        private readonly MessageProcessor _processor;
        private NetMQSocket _socket;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public TcpGateway(NetMQContext ctx, string endpoint, MessageProcessor processor)
        {
            _ctx = ctx;
            _endpoint = endpoint;
            _processor = processor;
            _socket = connect();
        }

        public void Reconnect()
        {
            _socket = connect();
        }

        public void Disconnect()
        {
            _socket.ReceiveReady -= socket_ReceiveReady;
            _socket.Options.Linger = TimeSpan.FromSeconds(0);
            _socket.Dispose();
            _socket = null;
        }

        private NetMQSocket connect()
        {
            try
            {
                return connect_raw();
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }

        private NetMQSocket connect_raw()
        {
            if (_socket != null)
            {
                Log.Debug("[TcpGateway] Disposing old socket. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
                _socket.ReceiveReady -= socket_ReceiveReady;
                _socket.Options.Linger = TimeSpan.FromSeconds(0);
                _socket.Dispose();
                _socket = null;
            }

            var spinner = new SpinWait();

            while (true)
            {
                Log.Info("[TcpGateway] Creating new socket. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
                var socket = _ctx.CreateRouterSocket();
                socket.ReceiveReady += socket_ReceiveReady;

                try
                {
                    socket.Bind(_endpoint);
                    Log.Debug("[TcpGateway] Socket connected at {0}. Thread Id: {1}", _endpoint, Thread.CurrentThread.ManagedThreadId);
                    return socket;
                }
                catch (NetMQException e)
                {
                    Log.Warn("[TcpGateway] Error binding to socket at {0}. Retrying in 500ms... Thread ID: {1}", e, _endpoint, Thread.CurrentThread.ManagedThreadId);
                    socket.Options.Linger = TimeSpan.FromSeconds(0);
                    socket.Dispose();
                    Thread.Sleep(500);
                    spinner.SpinOnce();
                }
            }
        }

        void socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMultipartMessage();
            Log.Debug("[TcpGateway] Received a message. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
            _processor.ProcessMessage(message, e.Socket);
        }

        public void Process(TaskCompleted completed)
        {
            completed.Send(_socket);
        }

        public bool ReceiveMessage()
        {
            try
            {
                if (_socket.Poll(TimeSpan.FromSeconds(0)))
                    return true;
            }
            catch (OperationCanceledException)
            {
                Log.Info("[TcpGateway] Cancellation signal received...exiting");
                throw;
            }
            catch (TerminatingException)
            {
                Log.Info("[TcpGateway] Context terminated...exiting");
                throw;
            }
            catch (Exception e)
            {
                Log.Warn("[TcpGateway] Error from mainloop...exiting", e);
                throw;
            }

            return false;
        }
    }
}