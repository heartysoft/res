namespace Res.Core.TcpTransport
{
    public class ErrorEntry
    {
        public int ErrorCode { get; private set; }
        public string Message { get; private set; }

        public ErrorEntry(int errorCode, string message)
        {
            ErrorCode = errorCode;
            Message = message;
        }
    }
}