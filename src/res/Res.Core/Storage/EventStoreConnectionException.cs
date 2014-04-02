using System;

namespace Res.Core.Storage
{
    public class EventStoreConnectionException : Exception
    {
        public EventStoreConnectionException(string connectionString, Exception innerException)
            : base(getMessage(connectionString), innerException) { }

        private static string getMessage(string connectionString)
        {
            return string.Format("A connection to the Event Store could not be made. The connection string used was: {0}{1}",
                Environment.NewLine, connectionString);
        }
    }
}