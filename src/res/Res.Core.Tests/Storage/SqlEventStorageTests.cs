using System.Configuration;
using System.Data.SqlClient;
using NUnit.Framework;
using Res.Core.Storage;

namespace Res.Core.Tests.Storage
{
    [Category("Slow")]
    public class SqlEventStorageTests : EventStorageTestBase
    {
        private static string ConnectionString = ConfigurationManager.ConnectionStrings["ResIntegrationTest"].ConnectionString;

        protected override void SetUpPerTest(EventStorage storage)
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
            }
        }

        protected override EventStorage GetStorage()
        {
            return new SqlEventStorage(ConnectionString);
        }
    }
}