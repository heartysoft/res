using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using cmdR;
using Res.Client;

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
            var engine = new ResEngine();
            engine.Start(endpoint);

            var subscriptionEngine = new ResSubscriptionEngine();   
            subscriptionEngine.Start(ConfigurationManager.AppSettings["res-sub"]);

            GlobalHack.SetEngine(engine);
            GlobalHack.SetSubscriptionEngine(subscriptionEngine);
            
            Console.WriteLine("Client engine started.");

            var cmdr = new CmdR("input>", new[] {"exit"});
            cmdr.AutoRegisterCommands();

            cmdr.Run(args);

            subscriptionEngine.Dispose();
            engine.Dispose();
            Console.WriteLine("Bye bye.");

        }
    }

    public static class GlobalHack
    {
        static private ResEngine _engine;
        private static ResSubscriptionEngine _subscriptionEngine;

        public static void SetEngine(ResEngine engine)
        {
            _engine = engine;
        }

        public static void SetSubscriptionEngine(ResSubscriptionEngine engine)
        {
            _subscriptionEngine = engine;
        }

        public static ResClient GetClient()
        {
            return _engine.CreateClient(TimeSpan.FromSeconds(10));
        }

        public static ResSubscriptionEngine GetSubscriptionEngine()
        {
            return _subscriptionEngine;
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

    public class SubscribeCommand : ICmdRCommand
    {
        public void Execute(IDictionary<string, string> param, CmdR cmdR)
        {
            var engine = GlobalHack.GetSubscriptionEngine();

            var subscriberId = param["subscriber"];
            var ctx = param["ctx"];
            var filter = param["filter"];
            var startTime = param["startTime"];

            var start = startTime == "now" ? DateTime.Now : DateTime.ParseExact(startTime, "dd-MM-yyyy hh:mm:ss", null);

            Action<SubscribedEvents> handler = evts =>
            {
                Console.WriteLine("[Subscriber - {0}]: received {1} events.", subscriberId, evts.Events.Length);
                evts.Done();
            };

            var sub = engine.Subscribe(subscriberId, new[] {new SubscriptionDefinition(ctx, filter)});
            sub.Start(handler, start, TimeSpan.FromSeconds(10), new CancellationToken());

            cmdR.State.CmdPrompt = "input>";
        }

        public string Command { get { return "subscribe subscriber ctx filter startTime"; } }

        public string Description
        {
            get
            {
                return
                    "subcribe to events. supported params: subscriber [id], ctx [context, res.dira is default for appends], filter [filter], startTime [now | dd-MM-yyyy hh:mm:ss]";
            }
        }
    }

    public class Appender
    {
        private readonly ResClient _client;

        public Appender(ResClient client)
        {
            _client = client;
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
                                var task = _client.CommitAsync("res.dira", "test-stream", events, ExpectedVersion.Any,
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
