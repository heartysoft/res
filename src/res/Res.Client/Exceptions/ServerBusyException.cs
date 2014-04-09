using System;

namespace Res.Client.Exceptions
{
    public class ServerBusyException : Exception
    {
        public ServerBusyException(string message) : base(message)
        {
        } 
    }
}