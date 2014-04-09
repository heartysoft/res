using System;

namespace Res.Client.Exceptions
{
    public class UnsupportedCommandException : Exception
    {
        public UnsupportedCommandException(string message) : base(message)
        {
        } 
    }
}