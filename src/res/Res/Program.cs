using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ.zmq;
using NLog;
using Res.Core;
using Res.Core.TcpTransport;
using SimpleConfig;
using Topshelf;

namespace Res
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        static int Main(string[] args)
        {
            System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            var result = HostFactory.Run(x =>
            {
                string endpoint = null;

                x.AddCommandLineDefinition("endpoint", s => endpoint = s);
                x.ApplyCommandLine();

                x.Service<ResServer>(s =>
                {
                    var config = Configuration.Load<ResConfiguration>();
                    if (string.IsNullOrWhiteSpace(endpoint) == false)
                    {
                        Console.WriteLine("setting endpoint...");
                        config.PublishEndpoint.Endpoint = endpoint;
                    }

                    s.ConstructUsing(name => new ResServer());
                    s.WhenStarted(rh => rh.Start(config));
                    s.WhenStopped(rh => rh.Dispose());
                });

                x.UseNLog();
            });

            return (int) result;
        }
    }
}
