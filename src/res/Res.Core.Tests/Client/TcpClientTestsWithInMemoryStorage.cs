namespace Res.Core.Tests.Client
{
    public class TcpClientTestsWithInMemoryStorage : TcpClientTests
    {
        protected override void FixtureSetup()
        {
        }

        protected override void FixtureTeardown()
        {
        }

        protected override void PerTestInit()
        {
            _harness = new ResHarness();
            _harness.StartInMemoryServer();
            _harness.StartClient();
        }

        protected override void PerTestCleanup()
        {
            _harness.Stop();
        }
    }
}