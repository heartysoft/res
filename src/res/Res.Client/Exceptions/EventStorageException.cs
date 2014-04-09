using System;

namespace Res.Client.Exceptions
{
    public class EventStorageException : Exception
    {
        public EventStorageException(string message) : base(message)
        {
        } 
    }
}