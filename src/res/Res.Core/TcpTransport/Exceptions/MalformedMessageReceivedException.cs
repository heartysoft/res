using System;

namespace Res.Core.TcpTransport.Exceptions
{
    public class MalformedMessageReceivedException : Exception
    {
        public MalformedMessageReceivedException(int frameCount) :
            base(string.Format("Received a message with {0} frames. Minimum expected frame count is {1}.", frameCount, 3))
        {
        }
    }
}