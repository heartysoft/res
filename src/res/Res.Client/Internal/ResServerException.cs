using System;

namespace Res.Client.Internal
{
    public class ResServerException : Exception
    {
        public string ErrorCode { get; private set; }

        public ResServerException(string errorCode, string message)
            : base (string.Format("[Res Server Error: {0}] {1}", errorCode, message))
        {
            ErrorCode = errorCode;
        }
    }
}