namespace Res.Client.Exceptions
{
    public class StorageReaderTimeoutException : ServerTimeoutException
    {
        public StorageReaderTimeoutException(string message) : base(message)
        {
        } 
    }
}