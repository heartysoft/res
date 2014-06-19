using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Res.Core.Storage
{
    public class SqlQueueStorage : QueueStorage
    {
        private readonly string _connectionString;

        public SqlQueueStorage()
            : this(ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString)
        {
        }

        public SqlQueueStorage(string connectionString)
        {
            _connectionString = connectionString;
        }

        public QueuedEvents Subscribe(SubscribeToQueue request)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("Queues_Subscribe", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("QueueId", request.QueueId);
                command.Parameters.AddWithValue("SubscriberId", request.SubscriberId);
                command.Parameters.AddWithValue("Context", request.Context);
                command.Parameters.AddWithValue("Filter", request.Filter);
                command.Parameters.AddWithValue("StartTime", request.UtcQueueStartTime);
                command.Parameters.AddWithValue("Count", request.AllocationSize);
                command.Parameters.AddWithValue("AllocationTimeInMilliseconds", request.AllocationTimeoutInMilliseconds);

                command.Connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    var events = new List<EventInStorage>();

                    while (reader.Read())
                    {
                        var @event = readEventInStorage(reader, 0);
                        events.Add(@event);
                    }

                    reader.NextResult();

                    long? allocationId = null;

                    if (reader.Read())
                        allocationId = (long) reader.GetSqlInt64(0);

                    return new QueuedEvents(allocationId, events.ToArray());
                }
            }
        }

        public QueuedEvents AcknowledgeAndFetchNext(AcknowledgeQueue request)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("Queues_Acknowledge", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("QueueId", request.QueueId);
                command.Parameters.AddWithValue("SubscriberId", request.SubscriberId);
                if (request.AllocationId.HasValue)
                    command.Parameters.AddWithValue("AllocationId", request.AllocationId.Value);
                command.Parameters.AddWithValue("Count", request.AllocationSize);
                command.Parameters.AddWithValue("AllocationTimeInMilliseconds", request.AllocationTimeInMilliseconds);

                command.Connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    var events = new List<EventInStorage>();

                    while (reader.Read())
                    {
                        var @event = readEventInStorage(reader, 0);
                        events.Add(@event);
                    }

                    reader.NextResult();

                    long? allocationId = null;

                    if (reader.Read())
                        allocationId = (long)reader.GetSqlInt64(0);

                    return new QueuedEvents(allocationId, events.ToArray());
                }
            }
        }

        private static EventInStorage readEventInStorage(IDataRecord reader, int startingOrdinal)
        {
            const int eventIdOrdinal = 0;
            const int streamIdOrdinal = 1;
            const int contextNameOrdinal = 2;
            const int sequenceOrdinal = 3;
            const int globalSequenceOrdinal = 4;
            const int timestampOrdinal = 5;
            const int eventTypeOrdinal = 6;
            const int bodyOrdinal = 7;

            var eventId = reader.GetGuid(eventIdOrdinal + startingOrdinal);
            var stream = reader.GetString(streamIdOrdinal + startingOrdinal);
            var contextName = reader.GetString(contextNameOrdinal + startingOrdinal);
            var sequence = (long)reader.GetValue(sequenceOrdinal + startingOrdinal);
            var globalSequence = (long)reader.GetValue(globalSequenceOrdinal + startingOrdinal);
            var timestamp = reader.GetDateTime(timestampOrdinal + startingOrdinal);
            var typeKey = reader.GetString(eventTypeOrdinal + startingOrdinal);
            var body = reader.GetString(bodyOrdinal + startingOrdinal);

            var @event = new EventInStorage(eventId, contextName, stream, sequence, globalSequence, timestamp, typeKey, body,
                null);

            return @event;
        }
    }
}