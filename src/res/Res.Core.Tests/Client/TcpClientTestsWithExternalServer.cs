using System.Configuration;
using System.Data.SqlClient;

namespace Res.Core.Tests.Client
{
    public class TcpClientTestsWithExternalServer : TcpClientTests
    {
        protected override void FixtureSetup()
        {
            _harness = new ResHarness();
            _harness.StartExternalServer();
            _harness.StartClient();
        }

        protected override void FixtureTeardown()
        {
            _harness.Stop();
        }

        protected override void PerTestInit()
        {
            ClearResStore();
        }

        protected override void PerTestCleanup()
        {
        }

        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["ResIntegrationTest"].ConnectionString;

        void ClearResStore()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();

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
    }
}