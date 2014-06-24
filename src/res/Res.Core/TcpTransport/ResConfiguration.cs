using System;
using System.Xml.Serialization;

namespace Res.Core.TcpTransport
{
    [XmlType("res")]
    public class ResConfiguration
    {
        [XmlElement("connectionStringName")]
        public string ConnectionStringName { get; set; }

        [XmlElement("reader")]
        public StorageBufferConfiguration Reader { get; set; }
        [XmlElement("writer")]
        public StorageBufferConfiguration Writer { get; set; }
        [XmlElement("queryEndpoint")]
        public QueryEndpointConfiguration QueryEndpoint { get; set; }

        [XmlElement("queueEndpoint")]
        public QueueEndpointConfiguration QueueEndpoint { get; set; }
        
        [XmlElement("publishEndpoint")]
        public PublishEndpointConfiguration PublishEndpoint { get; set; }
    }

    public class StorageBufferConfiguration
    {
        [XmlElement("bufferSize")]
        public int BufferSize { get; set; }
        [XmlElement("batchSize")]
        public int BatchSize { get; set; }
        [XmlElement("timeoutBeforeDrop")]
        public TimeSpan TimeoutBeforeDrop { get; set; }
    }

    public class QueryEndpointConfiguration
    {
        [XmlElement("endpoint")]
        public string Endpoint { get; set; }

        [XmlElement("bufferSize")]
        public int BufferSize { get; set; }

    }

    public class QueueEndpointConfiguration
    {
        [XmlElement("endpoint")]
        public string Endpoint { get; set; }

        [XmlElement("bufferSize")]
        public int BufferSize { get; set; }
    }

    public class PublishEndpointConfiguration
    {
        [XmlElement("endpoint")]
        public string Endpoint { get; set; }

        [XmlElement("bufferSize")]
        public int BufferSize { get; set; }
    }
}