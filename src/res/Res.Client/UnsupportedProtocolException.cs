using System;

namespace Res.Client
{
    public class UnsupportedProtocolException : Exception
    {
        public UnsupportedProtocolException(string protocol) : base(string.Format("Protocol {0} is not supported.", protocol))
        {
        }
    }
}