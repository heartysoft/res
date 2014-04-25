using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Res.Core.Storage
{
    public class SqlSubscriptionStorage : SubscriptionStorage
    {
        private readonly string _connectionString;

        public SqlSubscriptionStorage()
            : this(ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString)
        {
        }

        public SqlSubscriptionStorage(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SubscribeResponse[] Subscribe(IEnumerable<SubscribeRequest> requests)
        {
            var responses = requests.Select(doSubscribe);
            return responses.ToArray();
        }

        public EventInStorage[] FetchEvents(long subscriptionId, int suggestedCount, DateTime now)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("FetchEvents", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("SubscriptionId", subscriptionId);
                command.Parameters.AddWithValue("SuggestedCount", suggestedCount);
                command.Parameters.Add("CurrentTime", SqlDbType.DateTime2, 4).Value = now;


                command.Connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    var events = new List<EventInStorage>();

                    while (reader.Read())
                    {
                        var @event = readEventInStorage(reader, 0);
                        events.Add(@event);
                    }

                    return events.ToArray();
                }
            }
        }

        public void ProgressSubscription(long subscriptionId, DateTime expectedNextBookmark, DateTime now)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("ProgressSubscription", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("SubscriptionId", subscriptionId);
                command.Parameters.Add("ExpectedNextBookmark", SqlDbType.DateTime2, 4).Value = expectedNextBookmark;
                command.Parameters.Add("CurrentTime", SqlDbType.DateTime2, 4).Value = now;

                command.Connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static EventInStorage readEventInStorage(IDataRecord reader, int startingOrdinal)
        {
            const int eventIdOrdinal = 0;
            const int streamIdOrdinal = 1;
            const int contextNameOrdinal = 2;
            const int sequenceOrdinal = 3;
            const int timestampOrdinal = 4;
            const int eventTypeOrdinal = 5;
            const int bodyOrdinal = 6;

            var eventId = reader.GetGuid(eventIdOrdinal + startingOrdinal);
            var stream = reader.GetString(streamIdOrdinal + startingOrdinal);
            var contextName = reader.GetString(contextNameOrdinal + startingOrdinal);
            var sequence = (long)reader.GetValue(sequenceOrdinal + startingOrdinal);
            var timestamp = reader.GetDateTime(timestampOrdinal + startingOrdinal);
            var typeKey = reader.GetString(eventTypeOrdinal + startingOrdinal);
            var body = reader.GetString(bodyOrdinal + startingOrdinal);

            var @event = new EventInStorage(eventId, contextName, stream, sequence, timestamp, typeKey, body,
                null);

            return @event;
        }

        private SubscribeResponse doSubscribe(SubscribeRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("CreateSubscriptionIdempotent", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("Context", request.Context);
                command.Parameters.AddWithValue("Subscriber", request.SubscriberId);
                command.Parameters.Add("StartTime", SqlDbType.DateTime2, 4).Value = request.StartTime;
                command.Parameters.Add("CurrentTime", SqlDbType.DateTime2, 4).Value = request.CurrentTime;
                command.Parameters.AddWithValue("Filter", request.Filter);

                command.Connection.Open();
                var subscriptionId = (long)command.ExecuteScalar();

                return new SubscribeResponse(request.RequestId, subscriptionId);
            }
        }
    }
}