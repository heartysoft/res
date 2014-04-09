using System;

namespace Res.Client.Exceptions
{
    public class UnexpectedServerErrorException : Exception
    {
        public UnexpectedServerErrorException(string message) : base(message)
        {
        } 
    }
}