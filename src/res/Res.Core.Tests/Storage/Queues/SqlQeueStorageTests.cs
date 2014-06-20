using System.Configuration;
using System.Data.SqlClient;
using NUnit.Framework;
using Res.Core.Storage;

namespace Res.Core.Tests.Storage.Queues
{
    [Category("Slow")]
    public class SqlQeueStorageTests : QueueStorageTestBase
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["ResIntegrationTest"].ConnectionString;

        protected override EventStorage GetEventStorage()
        {
            return new SqlEventStorage(ConnectionString);
        }

        protected override void SetUpPerTest(QueueStorage storage, EventStorage eventStorage)
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();

                using (var cmd = new SqlCommand("truncate table Subscriptions;", sqlConnection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("truncate table EventWrappers;", sqlConnection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("truncate table Streams;", sqlConnection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("truncate table QueueAllocations;", sqlConnection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("truncate table Queues;", sqlConnection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        protected override QueueStorage GetQueueStorage()
        {
            return new SqlQueueStorage(ConnectionString);
        }
    }
}