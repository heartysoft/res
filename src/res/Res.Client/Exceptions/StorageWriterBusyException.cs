namespace Res.Client.Exceptions
{
    public class StorageWriterBusyException : ServerBusyException
    {
        public StorageWriterBusyException(string message) : base(message)
        {
        } 
    }
}