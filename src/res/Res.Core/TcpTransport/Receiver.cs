using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using NetMQ;

namespace Res.Core.TcpTransport
{
    public class Receiver
    {
        private readonly NetMQContext _ctx;
        private readonly string _endpoint;
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly MessageProcessor _processor;

        public Receiver(NetMQContext ctx, string endpoint, MessageProcessor messageProcessor)
        {
            _ctx = ctx;
            _endpoint = endpoint;
            _processor = messageProcessor;
        }

        public Task Start(CancellationToken token)
        {
            return Task.Factory.StartNew(()=>run(token), token);
        }

        private void run(CancellationToken token)
        {
            Logger.Info("[Receiver] Starting TCP receiver.");

            while (token.IsCancellationRequested == false)
            {
                try
                {
                    using (var socket = connect())
                    {
                        mainLoop(socket, token);
                    }
                }
                catch (TerminatingException)
                {
                    Logger.Info("[Receiver]Context terminated...exiting");
                }
                catch (Exception)
                {
                }
            }
        }

        private void mainLoop(NetMQSocket socket, CancellationToken token)
        {
            while (token.IsCancellationRequested == false)
            {
                var message = socket.ReceiveMessage();
                _processor.ProcessMessage(message, socket);
            }
        }

        private NetMQSocket connect()
        {
            Logger.InfoFormat("[Receiver] Binding to '{0}'.", _endpoint);
            var socket = _ctx.CreateRouterSocket();
            socket.Bind(_endpoint);
            Logger.InfoFormat("[Receiver] Bound to '{0}'.", _endpoint);
            return socket;
        }

    }
}