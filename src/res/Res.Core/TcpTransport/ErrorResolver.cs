using System;
using System.Collections.Generic;
using NLog;
using Res.Core.Storage;
using Res.Core.StorageBuffering;
using Res.Core.TcpTransport.Exceptions;

namespace Res.Core.TcpTransport
{
    public class ErrorResolver
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();


        private static readonly Dictionary<Type, Func<Exception, ErrorEntry>> Registry = new Dictionary<Type, Func<Exception, ErrorEntry>>
        {
            {typeof(MalformedMessageReceivedException), e => new ErrorEntry(1, e.ToString())},
            {typeof(UnsupportedProtocolException), e => new ErrorEntry(2, e.ToString())},
            {typeof(UnsupportedCommandException), e => new ErrorEntry(3, e.ToString())},
            {typeof(EventStorageWriter.StorageWriterBusyException), e => new ErrorEntry(4, e.ToString())},
            {typeof(EventStorageWriter.StorageWriterTimeoutException), e => new ErrorEntry(5, e.ToString())},
            {typeof(EventStorageWriter.ConcurrencyException), e => new ErrorEntry(6, e.ToString())},
            {typeof(EventStorageException), e => new ErrorEntry(7, e.ToString())},
            {typeof(EventStorageReader.EventNotFoundException), e => new ErrorEntry(8, e.ToString())},
            {typeof(EventStorageReader.StorageReaderTimeoutException), e => new ErrorEntry(9, e.ToString())},
            {typeof(EventStorageReader.StorageReaderBusyException), e => new ErrorEntry(10, e.ToString())},
        }; 

        public ErrorEntry GetError(Exception e)
        {
            if (e == null)
                return null;

            if (e is AggregateException)
                e = e.InnerException;

            var type = e.GetType();

            var entry = Registry[type];
            if (entry != null)
                return entry(e);

            return new ErrorEntry(-1, e.ToString());
        }
    }
}