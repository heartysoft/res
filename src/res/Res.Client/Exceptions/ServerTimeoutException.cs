using System;

namespace Res.Client.Exceptions
{
    public class ServerTimeoutException : Exception
    {
        public ServerTimeoutException(string message) : base(message)
        {
        } 
    }
}