using Res.Core.Storage;
using Res.Core.Storage.InMemoryQueueStorage;

namespace Res.Core.Tests.Storage.Queues
{
    public class InMemoryQueueStorageTests : QueueStorageTestBase
    {
        private InMemoryEventStorage _eventStorage;

        protected override QueueStorage GetQueueStorage()
        {
            return new InMemoryQueueStorage(_eventStorage);
        }

        protected override EventStorage GetEventStorage()
        {
            _eventStorage = new InMemoryEventStorage();
            return _eventStorage;
        }
    }
}