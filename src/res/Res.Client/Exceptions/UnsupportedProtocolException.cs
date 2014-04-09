using System;

namespace Res.Client.Exceptions
{
    public class UnsupportedProtocolException : Exception
    {
        public UnsupportedProtocolException(string message) : base(message)
        {
        } 
    }
}