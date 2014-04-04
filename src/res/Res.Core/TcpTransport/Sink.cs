using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;

namespace Res.Core.TcpTransport
{
    public class Sink
    {
        private readonly NetMQContext _ctx;
        private readonly string _address;
        readonly BlockingCollection<TaskCompleted> _completeds = new BlockingCollection<TaskCompleted>();

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
            return Task.Factory.StartNew(() =>
            {
                while (token.IsCancellationRequested == false)
                {
                    try
                    {
                        using (var socket = connect())
                        {
                            mainloop(socket, token);
                        }
                    }
                    catch (Exception e)
                    {
                        //TODO: Yip yip
                    }
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
            var socket = _ctx.CreateDealerSocket();
            socket.Bind(_address);
            return socket;
        }
    }
}