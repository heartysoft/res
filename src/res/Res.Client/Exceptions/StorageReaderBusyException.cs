namespace Res.Client.Exceptions
{
    public class StorageReaderBusyException : ServerBusyException
    {
        public StorageReaderBusyException(string message) : base(message)
        {
        }    
    }
}