using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cmdR;
using Res.Client;
using Res.Protocol;

namespace Res.Dira
{
    class Program
    {
        static void Main(string[] args)
        {
            string endpoint = null;

            if(args.Length > 0)
                if (args[0] == "-e")
                    endpoint = args[1];

            if (endpoint == null)
                endpoint = ConfigurationManager.AppSettings["res"];

            if(endpoint == null)
                throw new ArgumentException("Usage: res.dira.exe -e endpoint, or use an app setting named res with the endpoint as the value");

            Console.WriteLine("Hello...shall we try out the res server?");

            Console.WriteLine("Starting client engine for {0}...", endpoint);
            var engine = new ResPublishEngine(endpoint);

            GlobalHack.SetEngine(engine);
            
            Console.WriteLine("Client engine started.");

            var cmdr = new CmdR("input>", new[] {"exit"});
            cmdr.AutoRegisterCommands();

            cmdr.Run(args);

            engine.Dispose();
            Console.WriteLine("Bye bye.");
        }
    }

    public static class GlobalHack
    {
        static private ResPublishEngine _publishEngine;

        public static void SetEngine(ResPublishEngine publishEngine)
        {
            _publishEngine = publishEngine;
        }

        public static ResPublisher GetClient()
        {
            return _publishEngine.CreateRawPublisher(TimeSpan.FromSeconds(10));
        }
    }

    public class AppendEventsCommand : ICmdRCommand
    {
        public void Execute(IDictionary<string, string> param, CmdR cmdR)
        {
            int n = 1;
            
            if (param.ContainsKey("n"))
                n = int.Parse(param["n"]);

            var client = GlobalHack.GetClient();
            var appender = new Appender(client);
            appender.AppendEvents(n);

            cmdR.State.CmdPrompt = "input>";
        }

        public string Command { get { return "append n"; } }
        public string Description { get { return "append events. supported param: n [count]."; } }
    }

    public class Appender
    {
        private readonly ResPublisher _publisher;

        public Appender(ResPublisher publisher)
        {
            _publisher = publisher;
        }

        public void AppendEvents(int n)
        {
            Console.WriteLine("Appending {0} events...here we go...", n);

            var sw = new Stopwatch();

            sw.Start();

            appendEvents(n);

            sw.Stop();

            Console.WriteLine("Appended {0} events in {1} seconds at {2} events / sec.", n, sw.Elapsed.TotalSeconds, n / sw.Elapsed.TotalSeconds);
        }

        void appendEvents(int n)
        {
            var events = new[]
            {
                new EventData("test-order-placed", Guid.NewGuid(), "{}",
                    "{OrderId:25, Price:£200, Title:'Foo Bar', ProductId:'2500-12'", DateTime.Now)
            };
            
            var tasks = Enumerable.Range(1, n)
                .Select(
                    x =>
                    {
                        while (true)
                        {
                            try
                            {
                                var task = _publisher.CommitAsync("res.dira", "test-stream", events, ExpectedVersion.Any,
                                    TimeSpan.FromSeconds(10));
                                return task;
                            }
                            catch (InternalBufferOverflowException)
                            {
                                Task.Delay(100).Wait();
                            }
                        }
                    }).ToList();

            Task.WhenAll(tasks).Wait();

        }
    }
}
