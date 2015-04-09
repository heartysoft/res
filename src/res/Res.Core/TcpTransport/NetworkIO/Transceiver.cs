using System;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NLog;

namespace Res.Core.TcpTransport.NetworkIO
{
    public class Transceiver
    {
        private readonly Func<TcpGateway> _gatewayFactory;
        private readonly OutBuffer _buffer;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Transceiver(Func<TcpGateway> gatewayFactory, OutBuffer buffer)
        {
            _gatewayFactory = gatewayFactory;
            _buffer = buffer;
        }

        public Task Start(CancellationToken token)
        {
            return Task.Factory.StartNew(() => run(token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void run(CancellationToken token)
        {
            Log.Info("[Transceiver] Starting up.");

            var gateway = _gatewayFactory();

            while (token.IsCancellationRequested == false)
            {
                try
                {
                    var resultSent = sendResponse(gateway);
                    var newRequest = receiveMessage(gateway);

                    if (!resultSent && !newRequest)
                    {
                        Thread.Sleep(10);
                    }
                }
                catch (OperationCanceledException)
                {
                    Log.Info("[Transceiver] Cancellation signal received...exiting");
                    break;
                }
                catch (TerminatingException)
                {
                    Log.Info("[Tranceiver] Context terminated...exiting");
                    break;
                }
                catch (Exception e)
                {
                    Log.Warn("[Transceiver] Error from mainloop.", e);
                    gateway.Reconnect();
                }
            }

            gateway.Disconnect();


            Log.Info("[Transceiver] Exiting, bye bye.");
        }

        private bool sendResponse(TcpGateway gateway)
        {
            TaskCompleted completed;

            if (!_buffer.Poll(out completed)) return false;

            gateway.Process(completed);
            return true;
        }

        private static bool receiveMessage(TcpGateway gateway)
        {
            bool newRequest = gateway.ReceiveMessage();
            return newRequest;
        }
    }
}