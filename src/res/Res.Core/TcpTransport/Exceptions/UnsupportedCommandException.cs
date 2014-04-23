using System;

namespace Res.Core.TcpTransport.Exceptions
{
    public class UnsupportedCommandException : Exception
    {
        public UnsupportedCommandException(string command, string protocol)
            : base(string.Format("Command {0} is not supported under protocol {1}", command, protocol))
        {

        }
    }
}