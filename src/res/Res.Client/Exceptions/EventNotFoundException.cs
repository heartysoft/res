using System;

namespace Res.Client.Exceptions
{
    public class EventNotFoundException : Exception
    {
        public EventNotFoundException(string message) : base(message)
        {
        } 
    }
}