using System;

namespace Res.Client.Internal
{
    public class ErrorResolver
    {
        public static void RaiseExceptionIfNeeded(string errorCode, string message, Action<Exception> raise)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
                return;

            raise(new ResServerException(errorCode, message));
        }
    }
}