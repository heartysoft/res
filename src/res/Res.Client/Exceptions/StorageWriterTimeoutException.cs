namespace Res.Client.Exceptions
{
    public class StorageWriterTimeoutException : ServerTimeoutException
    {
        public StorageWriterTimeoutException(string message) : base(message)
        {
        } 
    }
}