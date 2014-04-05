using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using NetMQ;

namespace Res.Core.TcpTransport
{
    public class Sink
    {
        private readonly NetMQContext _ctx;
        private readonly string _address;
        readonly BlockingCollection<TaskCompleted> _completeds = new BlockingCollection<TaskCompleted>();
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        public Sink(NetMQContext ctx, string address)
        {
            _ctx = ctx;
            _address = address;
        }

        public void EnqueResult(TaskCompleted completed)
        {
            _completeds.Add(completed);
        }

        public Task Start(CancellationToken token)
        {
            return Task.Factory.StartNew(() => run(token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void run(CancellationToken token)
        {
            Logger.Info("[Sink] Starting Sink.");

            while (token.IsCancellationRequested == false)
            {
                try
                {
                    using (var socket = connect())
                    {
                        mainloop(socket, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.Info("[Sink] Cancellation signal received...exiting");
                    break;
                }
                catch (TerminatingException)
                {
                    Logger.Info("[Sink] Context terminated...exiting");
                    break;
                }
                catch (Exception e)
                {
                    Logger.Warn("[Sink] Error from mainloop.", e);
                }
            }

            _completeds.Dispose();
            Logger.Info("[Sink] Sink, signing off.");
        }

        private void mainloop(NetMQSocket socket, CancellationToken token)
        {
            while (token.IsCancellationRequested == false)
            {
                var completed = _completeds.Take(token);
                completed.Send(socket);
            }
        }

        private NetMQSocket connect()
        {
            Logger.InfoFormat("[Sink] Conneting to '{0}'.", _address);
            var socket = _ctx.CreateDealerSocket();
            try
            {
                socket.Connect(_address);
                Logger.InfoFormat("[Sink] Conneceted to '{0}'.", _address);
                return socket;
            }
            catch(Exception)
            {
                socket.Dispose();
                throw;
            }
        }
    }
}