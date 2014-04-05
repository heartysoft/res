using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
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
                    s.ConstructUsing(name => new ResHost());
                    s.WhenStarted(rh => rh.Start());
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
