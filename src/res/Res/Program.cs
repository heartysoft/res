using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using SimpleConfig;
using Topshelf;
using Topshelf.Common.Logging;

namespace Res
{
    class Program
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                Logger.Error("Hello start");
                x.Service<ResHost>(s =>
                {
                    var config = Configuration.Load<ResConfiguration>();
                    s.ConstructUsing(name => new ResHost());
                    s.WhenStarted(rh => rh.Start(config));
                    s.WhenStopped(rh => rh.Stop());
                });

                x.RunAsLocalSystem();

                x.SetDescription("Res");
                x.SetDisplayName("Res");
                x.SetServiceName("Res");

                x.UseCommonLogging();
            });
        }
    }
}
