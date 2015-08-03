using System;

namespace Res.Client.Exceptions
{
    public class QueueAlreadyExistsInContextWithDifferentFilterException
        :Exception
    {
        public QueueAlreadyExistsInContextWithDifferentFilterException(string message) : base(message)
        {
        }
    }
}