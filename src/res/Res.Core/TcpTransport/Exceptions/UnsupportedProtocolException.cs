using System;

namespace Res.Core.TcpTransport.Exceptions
{
    public class UnsupportedProtocolException : Exception
    {
        public UnsupportedProtocolException(string protocol, params string[] requiredProtocol)
            : base(
                string.Format("Received request with protocol: {0}. Supported protocols: {1}", protocol,
                    string.Join(", ", requiredProtocol)))
        {
        }
    }
}