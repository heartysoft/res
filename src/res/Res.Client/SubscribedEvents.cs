using System.Threading.Tasks;

namespace Res.Client
{
    public class SubscribedEvents
    {
        public EventInStorage[] Events { get; private set; }
        public Task Completed { get { return _done.Task; } }

        readonly TaskCompletionSource<bool> _done = new TaskCompletionSource<bool>(); 

        public SubscribedEvents(EventInStorage[] events)
        {
            Events = events;
        }

        public void Done()
        {
            _done.TrySetResult(true);
        }
    }
}