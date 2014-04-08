using System;

namespace Res.Client.Internal
{
    public class UnsupportedProtocolException : Exception
    {
        public UnsupportedProtocolException(string protocol) : base(string.Format("Protocol {0} is not supported.", protocol))
        {
        }
    }
}