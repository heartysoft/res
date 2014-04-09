using System;

namespace Res.Client.Exceptions
{
    public class MalformedMessageException : Exception
    {
        public MalformedMessageException(string message) : base(message)
        {
        } 
    }
}