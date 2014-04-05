using System;
using System.Configuration;
using System.Xml.Serialization;

namespace Res
{
    [XmlType("res")]
    public class ResConfiguration
    {
        [XmlElement("endpoint")]
        public string TcpEndpoint { get; set; }

        [XmlElement("connectionStringName")]
        public string ConnectionStringName { get; set; }

        [XmlElement("reader")]
        public StorageBufferConfiguration Reader { get; set; }
        [XmlElement("writer")]
        public StorageBufferConfiguration Writer { get; set; }
    }

    public class StorageBufferConfiguration
    {
        [XmlElement("bufferSize")]
        public int BufferSize { get; set; }
        [XmlElement("bacthSize")]
        public int BatchSize { get; set; }
        [XmlElement("timeoutBeforeDrop")]
        public TimeSpan TimeoutBeforeDrop { get; set; }
    }
}