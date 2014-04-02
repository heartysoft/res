using Res.Core.Storage;

namespace Res.Core.Tests.Storage
{
    public class InMemoryEventStorageTests : EventStorageTestBase
    {
        protected override EventStorage GetStorage()
        {
            return new InMemoryEventStorage();
        }
    }
}