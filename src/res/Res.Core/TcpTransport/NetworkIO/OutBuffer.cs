using System;
using System.Collections.Concurrent;

namespace Res.Core.TcpTransport.NetworkIO
{
    public class OutBuffer
    {
        readonly BlockingCollection<TaskCompleted> _completeds;

        public OutBuffer(int maxSizeAsPowerOfTwo)
        {
            _completeds = new BlockingCollection<TaskCompleted>((int)Math.Pow(2, maxSizeAsPowerOfTwo));
        }

        public bool Offer(TaskCompleted completed)
        {
            return _completeds.TryAdd(completed);
        }

        public void OfferAndWaitUntilAccepted(TaskCompleted completed)
        {
            _completeds.Add(completed);
        }

        public bool Poll(out TaskCompleted completed)
        {
            return _completeds.TryTake(out completed);
        }
    }
}