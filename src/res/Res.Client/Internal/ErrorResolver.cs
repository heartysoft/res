using System;
using Res.Client.Exceptions;

namespace Res.Client.Internal
{
    public class ErrorResolver
    {
        public static void RaiseException(string errorCode, string message, Action<Exception> raise)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
                return;

            switch (errorCode)
            {
                case "-1": raise(new UnexpectedServerErrorException(message)); break;
                case "1": raise(new MalformedMessageException(message)); break;
                case "2": raise(new UnsupportedProtocolException(message)); break;
                case "3": raise(new UnsupportedCommandException(message)); break;
                case "4": raise(new StorageWriterBusyException(message)); break;
                case "5": raise(new StorageWriterTimeoutException(message)); break;
                case "6": raise(new ConcurrencyException(message)); break;
                case "7": raise(new EventStorageException(message)); break;
                case "8": raise(new EventNotFoundException(message)); break;
                case "9": raise(new StorageReaderTimeoutException(message)); break;
                case "10": raise(new StorageReaderBusyException(message)); break;

                default:
                    raise(new ResServerException(errorCode, message));
                    break;
            }

        }
    }


}