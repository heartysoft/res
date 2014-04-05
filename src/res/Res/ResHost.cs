using System;
using Common.Logging;

namespace Res
{
    public class ResHost
    {
        private static ILog _logger = LogManager.GetCurrentClassLogger();
        public void Start()
        {
            Console.WriteLine("Started.");
            _logger.Error("Whhoop");
        }

        public void Stop()
        {
            Console.WriteLine("Stopped");
            _logger.Info("deee");
        }
    }
}