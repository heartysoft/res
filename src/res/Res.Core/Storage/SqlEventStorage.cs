using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Res.Core.Storage
{
    public class SqlEventStorage : EventStorage
    {
        private readonly string _connectionString;
        private static readonly Dictionary<string, string> DatabaseChecks = new Dictionary<string, string> {
            {"SELECT 1","Could not connect to SQL Server instance with supplied configuration."},
            {"SELECT isnull(has_perms_by_name('dbo.EventWrappers', 'OBJECT', 'SELECT'), 0)","Does not have SELECT permissions on the 'dbo.EventWrappers' table."},
            {"SELECT isnull(has_perms_by_name('dbo.EventWrappers', 'OBJECT', 'INSERT'), 0)","Does not have INSERT permissions on the 'dbo.EventWrappers' table."},
            {"SELECT isnull(has_perms_by_name('dbo.EventWrappers', 'OBJECT', 'UPDATE'), 0)","Does not have UPDATE permissions on the 'dbo.EventWrappers' table."},
            {"SELECT isnull(has_perms_by_name('dbo.EventWrappers', 'OBJECT', 'DELETE'), 0)","Does not have DELETE permissions on the 'dbo.EventWrappers' table."},
            {"SELECT isnull(has_perms_by_name('dbo.Streams', 'OBJECT', 'SELECT'), 0)","Does not have SELECT permissions on the 'dbo.Streams' table."},
            {"SELECT isnull(has_perms_by_name('dbo.Streams', 'OBJECT', 'INSERT'), 0)","Does not have INSERT permissions on the 'dbo.Streams' table."},
            {"SELECT isnull(has_perms_by_name('dbo.Streams', 'OBJECT', 'UPDATE'), 0)","Does not have UPDATE permissions on the 'dbo.Streams' table."},
            {"SELECT isnull(has_perms_by_name('dbo.Streams', 'OBJECT', 'DELETE'), 0)","Does not have DELETE permissions on the 'dbo.Streams' table."},
        };

        public SqlEventStorage()
            : this(ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString)
        {
        }

        public SqlEventStorage(string connectionString)
        {
            _connectionString = connectionString;
        }


        public EventInStorage[] LoadEvents(string context, object streamId, long fromVersion = 0, long? maxVersion = null)
        {
            try
            {
                return doLoadEvents(context, streamId, fromVersion, maxVersion);
            }
            catch (Exception e)
            {
                throw new EventStorageException(e);
            }
        }

        public Dictionary<Guid, EventInStorage> FetchEvent(FetchEventRequest[] request)
        {
            try
            {
                return doFetchEvents(request);
            }
            catch (Exception e)
            {
                throw new EventStorageException(e);
            }
        }

        public CommitResults Store(CommitsForStorage commits)
        {
            try
            {
                return doStore(commits);
            }
            catch (Exception e)
            {
                throw new EventStorageException(e);
            }
        }

        private Dictionary<Guid, EventInStorage> doFetchEvents(FetchEventRequest[] request)
        {
            var results = new Dictionary<Guid, EventInStorage>();

            var parameter = getFetchParameter(request);

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("FetchEvent", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(parameter);

                try
                {
                    command.Connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var requestId = reader.GetGuid(0);
                            var e = readEventInStorage(reader, 1);

                            results[requestId] = e;
                        }

                        return results;
                    }
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
        }

        private CommitResults doStore(CommitsForStorage commits)
        {
            var eventsParameter = getEventsParameter(commits);

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("AppendEvents", connection))
            {
                var unsuccessful = new List<Guid>();

                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(eventsParameter);

                try
                {
                    command.Connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            unsuccessful.Add(reader.GetGuid(0));
                        }

                        var unsuccessfulCommits = unsuccessful.ToArray();
                        var successfulCommits =
                            commits.
                                Commits.Where(x => unsuccessful.Contains(x.CommitId) == false)
                                .Select(x => x.CommitId).ToArray();

                        return new CommitResults(successfulCommits, unsuccessfulCommits);
                    }
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
        }

        private EventInStorage[] doLoadEvents(string context, object streamId, long fromVersion, long? maxVersion)
        {
            var events = new List<EventInStorage>();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("LoadEvents", connection))
            {

                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("Context", context);
                command.Parameters.AddWithValue("Stream", streamId);

                if (fromVersion != 0)
                    command.Parameters.AddWithValue("FromVersion", fromVersion);
                if (maxVersion.HasValue)
                    command.Parameters.AddWithValue("ToVersion", maxVersion);

                try
                {
                    command.Connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var @event = readEventInStorage(reader, 0);
                            events.Add(@event);
                        }

                        return events.ToArray();
                    }
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
        }

        private EventInStorage readEventInStorage(SqlDataReader reader, int startingOrdinal)
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

        private SqlParameter getFetchParameter(FetchEventRequest[] requestses)
        {
            var table = getEmptyFetchEventsTable();
            addFetchRequestsToTable(requestses, table);
            var parameter = new SqlParameter("Events", SqlDbType.Structured);
            parameter.Value = table;
            parameter.TypeName = "EventRequestParameter";
            return parameter;
        }

        private void addFetchRequestsToTable(FetchEventRequest[] requestses, DataTable table)
        {
            foreach (var request in requestses)
            {
                table.Rows.Add(request.EventId, request.Stream, request.Context, request.RequestId);
            }
        }

        private SqlParameter getEventsParameter(CommitsForStorage commits)
        {
            var eventsTable = getEmptyEventsTable();
            addCommitsToTable(commits, eventsTable);
            var parameter = new SqlParameter("Events", SqlDbType.Structured);
            parameter.Value = eventsTable;
            parameter.TypeName = "EventParameter";
            return parameter;
        }

        private void addCommitsToTable(CommitsForStorage commits, DataTable table)
        {
            foreach (var commit in commits.Commits)
                foreach (var e in commit.Events)
                    table.Rows.Add(e.EventId, commit.Stream, commit.Context, e.Sequence, e.Timestamp, e.TypeKey, e.Body,
                                   commit.CommitId);
        }

        private DataTable getEmptyFetchEventsTable()
        {
            var table = new DataTable("Events");

            table.Columns.Add("EventId", typeof(Guid));
            table.Columns.Add("StreamId", typeof(string));
            table.Columns.Add("ContextName", typeof(string));
            table.Columns.Add("RequestId", typeof(Guid));

            return table;
        }

        private DataTable getEmptyEventsTable()
        {
            var table = new DataTable("Events");

            table.Columns.Add("EventId", typeof(Guid));
            table.Columns.Add("StreamId", typeof(string));
            table.Columns.Add("ContextName", typeof(string));
            table.Columns.Add("Sequence", typeof(long));
            table.Columns.Add("TimeStamp", typeof(DateTime));
            table.Columns.Add("EventType", typeof(string));
            table.Columns.Add("Body", typeof(string));
            table.Columns.Add("CommitId", typeof(Guid));

            return table;
        }

        public void Verify()
        {
            checkForDatabasePermissions();
        }

        private void checkForDatabasePermissions()
        {
            try
            {
                using (var connection = new SqlConnection(this._connectionString))
                {
                    connection.Open();

                    foreach (var item in DatabaseChecks)
                    {
                        using (var command = new SqlCommand(item.Key, connection))
                        {

                            var result = (int)command.ExecuteScalar();

                            if (result != 1)
                            {
                                throw new Exception(item.Value);
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (SqlException sex)
            {
                throw new EventStoreConnectionException(_connectionString, sex);
            }
        }
    }
}