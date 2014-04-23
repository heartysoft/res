using System;
using System.Threading;
using Common.Logging;
using NetMQ;

namespace Res.Core.TcpTransport.NetworkIO
{
    public class TcpGateway
    {
        private readonly NetMQContext _ctx;
        private readonly string _endpoint;
        private readonly MessageProcessor _processor;
        private NetMQSocket _socket;
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

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
            if (_socket != null)
            {
                Log.InfoFormat("[TcpGateway] Disposing old socket. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
                _socket.ReceiveReady -= socket_ReceiveReady;
                _socket.Options.Linger = TimeSpan.FromSeconds(0);
                _socket.Dispose();
                _socket = null;
            }

            Log.InfoFormat("[TcpGateway] Creating new socket. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
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
                catch (NetMQException e)
                {
                    Log.WarnFormat("[TcpGateway] Error connecting to socket. Retrying... Thread ID: {0}", e, Thread.CurrentThread.ManagedThreadId);
                    socket.Options.Linger = TimeSpan.FromSeconds(0);
                    socket.Dispose();
                    spinner.SpinOnce();
                }
            }
        }

        void socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMessage();
            _processor.ProcessMessage(message, _socket);
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
                Log.Warn("[TcpGateway] Error from mainloop.", e);
                throw;
            }

            return false;
        }
    }
}